using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using BL.Helpers;
using DAL;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BL
{
    public class InitGroom
    {
        public static EventEntities DB = new EventEntities();
        public static WebResult<bool> InsertGroom()
        {
            try
            {
                HttpResponseMessage response = new HttpResponseMessage();
                var httpRequest = HttpContext.Current.Request;
                List<Entities.ImageEntity> images = new List<Entities.ImageEntity>();
                if (httpRequest.Files.Count == 1)
                {

                    var postedFile = httpRequest.Files[0];
                    if (string.Equals(postedFile.ContentType, "image/jpg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/jpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/pjpeg", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/gif", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/x-png", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(postedFile.ContentType, "image/png", StringComparison.OrdinalIgnoreCase))
                    {
                        string urlStorage = InitImages.SendToStorage(postedFile.FileName, postedFile.InputStream);
                        if (urlStorage == "")
                            return new WebResult<bool>()
                            {
                                Status = false,
                                Message = "failed to load image",
                                Value = false
                            };
                        Groom groom = new Groom();
                        groom.url = urlStorage;
                        string token = getFaceToken(urlStorage);
                        if (token == "")
                            return new WebResult<bool>()
                            {
                                Status=false,
                                Message="failed",
                                Value=false
                            };
                        groom.token = token;
                        groom.name = postedFile.FileName;
                        DB.Grooms.Add(groom);
                        if (DB.SaveChanges() > 0)
                            return new WebResult<bool>()
                            {
                                Status = true,
                                Message = "Ok",
                                Value = true
                            };
                    }
                }

                return new WebResult<bool>()
                {
                    Status = false,
                    Message = "That's not an image",
                    Value = false
                };

            }
            catch (Exception e)
            {
                return new WebResult<bool>()
                {
                    Status = false,
                    Message = e.Message,
                    Value = false
                };
            }
        }
        private static string getFaceToken(string url)
        {

            const string API_Key = "Yoxjj0Tu2hUPY5D5K-iQ4ZkJoGm2W2r3";
            const string API_Secret = "q97dj2QUarKmaC5NdTOdBXjC4XI2USta";
            const string BaseUrl = "https://api-us.faceplusplus.com/facepp/v3/detect";
            IRestClient _client = new RestClient(BaseUrl);
            RestRequest request = new RestRequest(Method.POST);
            request.AddParameter("api_key", API_Key);
            request.AddParameter("api_secret", API_Secret);
            request.AddParameter("image_url", url);
            request.AddParameter("return_attributes", "eyestatus");
            try
            {
                var response = _client.Execute(request);
                JObject results = JObject.Parse(response.Content);
                string faceToken = results["faces"].First.Last.First.ToString();
                return faceToken;

            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}