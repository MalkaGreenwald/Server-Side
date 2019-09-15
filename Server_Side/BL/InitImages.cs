using BL.Helpers;
using Clarifai.API;
using Clarifai.DTOs.Inputs;
using Clarifai.DTOs.Predictions;
using DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Google.Cloud.Vision.V1;
using Google.Cloud.Storage.V1;
using Entities;
using RestSharp;

namespace BL
{
    public class InitImages
    {
        //Clarifai המפתח, של מלכי גרינוולד
        static ClarifaiClient clarifaiClient = new ClarifaiClient("15d9d0972ffb42009c5d3b757c55d679");
        static List<Concept> resClarifai = new List<Concept>();
        //Microsoft vision api המפתח, של מלכי גרינוולד
        const string MicrosoftKey = "26575440cd95406b888237d955b11383";
        //Microsoft vision api endpoint
        const string uriBase =
        "https://event-photos.cognitiveservices.azure.com/face/v1.0/detect";
        public static EventEntities DB = new EventEntities();

        public static string SendToStorage(string fileName, Stream stream)
        {
            try
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", @"C:\My First Project-b781c7f56bda.json");
                string bucketName = "bucketmyexample";
                string imageURL = "https://storage.googleapis.com/bucketmyexample/" + fileName;
                StorageClient storage = StorageClient.Create();
                var res = storage.UploadObject(bucketName, fileName, null, stream);
                Console.WriteLine($"Uploaded {fileName}.");
                return imageURL;

            }
            catch (Exception)
            {
                return "";
            }
        }
        private static async Task<List<Concept>> GetResClarifai(string fileUrl)
        {

            try
            {
                var res = await clarifaiClient.PublicModels.GeneralModel
                .Predict(new ClarifaiURLImage(fileUrl))
                .ExecuteAsync();
                return res.Get().Data;
            }
            catch (Exception)
            {
                return null;
            }
        }
        private static JObject getResultFacePP(string filePath)
        {
            const string API_Key = "Yoxjj0Tu2hUPY5D5K-iQ4ZkJoGm2W2r3";
            const string API_Secret = "q97dj2QUarKmaC5NdTOdBXjC4XI2USta";
            const string BaseUrl = "https://api-us.faceplusplus.com/facepp/v3/detect";
            IRestClient _client = new RestClient(BaseUrl);
            RestRequest request = new RestRequest(Method.POST);
            request.AddParameter("api_key", API_Key);
            request.AddParameter("api_secret", API_Secret);
            request.AddParameter("image_url", filePath);
            request.AddParameter("return_attributes", "eyestatus");
            try
            {
                var response = _client.Execute(request);
                JObject results = JObject.Parse(response.Content);
                return results;
            }
            catch (Exception)
            {
                return null;
            }
        }
        public static async Task<WebResult<List<ImageEntity>>> InsertImages()
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                var httpRequest = HttpContext.Current.Request;
                string uploaded_image;
                List<ImageEntity> images;
                if (httpRequest.Files.Count > 0)
                {
                    for (var i = 0; i < httpRequest.Files.Count; i++)
                    {
                        var postedFile = httpRequest.Files[i];
                        if (string.Equals(postedFile.ContentType, "image/jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/pjpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/gif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/x-png", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/png", StringComparison.OrdinalIgnoreCase))
                        {
                            uploaded_image = SendToStorage(postedFile.FileName, postedFile.InputStream);
                            bool suc = await InitImageDetailsAsync(uploaded_image, postedFile.FileName);
                            if (suc == false)
                            {
                                images = Images.GetImages().Value;
                                return new WebResult<List<ImageEntity>>()
                                {
                                    Status = false,
                                    Message = "failed init detailes",
                                    Value = images
                                };
                            }
                        }
                    }
                }
                images = Images.GetImages().Value;
                if(images==null)
                {
                    return new WebResult<List<ImageEntity>>()
                    {
                        Status = false,
                        Message = "failed load images",
                        Value = images
                    };
                }
                return new WebResult<List<ImageEntity>>()
                {
                    Status = true,
                    Message = "Ok",
                    Value = images
                };
            }
            catch (Exception e)
            {
                return new WebResult<List<ImageEntity>>()
                {
                    Status = false,
                    Message = e.Message,
                    Value = null
                };
            }
        }
        //הפונקציה מוסיפה שורה לטבלת התמונות, תמונה עם כל הפרטים עליה
        private static async Task<bool> InitImageDetailsAsync(string fileUrl, string fileName)
        {
            try
            {
                image img = new image();
                img.url = fileUrl;
                img.name = fileName;
                var resClarifai = await GetResClarifai(fileUrl);
                var resFacePP = getResultFacePP(fileUrl);
                if (resClarifai == null || resFacePP == null)
                    return false;
                img.isBlur = IsBlur(resClarifai);
                img.isClosedEye = IsClosedEye(resFacePP);
                img.isGroom = IsGroom(resFacePP);
                img.numPerson = await NumPerson(fileUrl);
                if (img.numPerson == -1)
                    return false;
                img.isIndoors = IsIndoors(resClarifai);
                img.isOutdoors = IsOutdoors(resClarifai);
                img.hasChildren = HasChildren(resClarifai);
                DB.images.Add(img);
                if(DB.SaveChanges()>0)
                    return true;
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool IsBlur(List<Concept> res)
        {

            foreach (var concept in res)
                if (concept.Name == "blur")
                    return true;
            return false;
        }

        private static bool IsClosedEye(JObject results)
        {
            //face++
            //רשימה של פנים:
            var faces = results["faces"].Children().ToList();
            int i = 1;
            foreach (var item in faces)
            {
                if (++i > 5)
                    return false;
                var eyestatus = item["attributes"]["eyestatus"];
                var jsonData = eyestatus.Children().ToList();
                List<JToken> tokens = jsonData.Children().ToList();
                if (double.Parse(tokens[0]["no_glass_eye_close"].ToString()) > 1)
                    return true;
                if (double.Parse(tokens[0]["normal_glass_eye_close"].ToString()) > 1)
                    return true;
                if (double.Parse(tokens[1]["no_glass_eye_close"].ToString()) > 1)
                    return true;
                if (double.Parse(tokens[1]["normal_glass_eye_close"].ToString()) > 1)
                    return true;

            }
            return false;
        }

        private static bool IsGroom(JObject results)
        {
            //face++
            const string API_Key = "Yoxjj0Tu2hUPY5D5K-iQ4ZkJoGm2W2r3";
            const string API_Secret = "q97dj2QUarKmaC5NdTOdBXjC4XI2USta";
            const string BaseUrl = "https://api-us.faceplusplus.com/facepp/v3/compare";
            IRestClient _client;
            RestRequest request;
            string token1 = DB.Grooms.First(f => f.name == "groom.jpg").token;
            var faces = results["faces"].Children().ToList();
            foreach (var item in faces)
            {
                string token2 = item.Last.First.ToString();
                _client = new RestClient(BaseUrl);
                request = new RestRequest(Method.POST);
                request.AddParameter("api_key", API_Key);
                request.AddParameter("api_secret", API_Secret);
                request.AddParameter("face_token1", token1);
                request.AddParameter("face_token2", token2);
                var response = _client.Execute(request);
                JObject res = JObject.Parse(response.Content);
                if (double.Parse(res["confidence"].ToString()) > 80)
                    return true;
            }
            return false;
        }

        private static bool IsIndoors(List<Concept> res)
        {
            foreach (var concept in res)
                if (concept.Name == "indoors" || concept.Name == "interior design")
                    return true;
            return false;
        }

        private static bool IsOutdoors(List<Concept> res)
        {
            decimal num = Convert.ToDecimal(0.93);
            bool flag = false;
            foreach (var concept in res)
            {
                if (concept.Name == "indoors")
                    return false;
                if (concept.Name == "nature" && concept.Value >= num
                    || concept.Name == "beach" || concept.Name == "street"
                    || concept.Name == "sun" || concept.Name == "sky" || concept.Name == "city")
                    flag = true;
            }
            return flag;
        }


        private static bool HasChildren(List<Concept> res)
        {
            decimal num = Convert.ToDecimal(0.9);
            foreach (var concept in res)
            {
                if (concept.Name == "child" || concept.Name == "girl" || concept.Name == "baby" || concept.Name == "boy")
                    if (concept.Value > num)
                        return true;
            }
            return false;
        }

        //Clarifai פונקציה זו ניגשת ל
        //כי זה מודל לא גנרלי api צריך כאן לגשת מחדש ל
        private static async Task<int> NumPerson(string fileUrl)
        {
            try
            {
                var res = await clarifaiClient.PublicModels.FaceDetectionModel
                .Predict(new ClarifaiURLImage(fileUrl))
                .ExecuteAsync();
                return res.Get().Data.Count;
            }
            catch (Exception)
            {
                return -1;
            }

        }
    }
}

