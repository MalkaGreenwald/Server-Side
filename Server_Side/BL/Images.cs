using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BL.Helpers;
using DAL;
using Entities;

namespace BL
{
    public class Images
    {
        public static EventEntities DB = new EventEntities();
        public static WebResult<List<ImageEntity>> GetImages()
        {
            try
            {
                List<ImageEntity> listEntity = new List<ImageEntity>();
                if (DB.images.Count() > 0)
                {
                    List<image> listImage = DB.images.Where(img => img.isInRecycleBin == null || img.isInRecycleBin == false).ToList();
                    if (listImage != null)
                        foreach (var image in listImage)
                            listEntity.Add(Casting.ImageCast.GetImageEntity(image));
                }
                return new WebResult<List<ImageEntity>>()
                {
                    Status=true,
                    Message="Ok",
                    Value=listEntity
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

        public static WebResult<bool> DeleteImg(string url)
        {
            try
            {
                DB.images.Where(image => image.url == url).ToList().ForEach(f => f.isInRecycleBin = true);
                DB.SaveChanges();
                return new WebResult<bool>()
                {
                    Status = true,
                    Message = "Ok",
                    Value = true
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

        public static WebResult<List<ImageEntity>> getRecycleBin()
        {
            try
            {
                List<ImageEntity> rec = new List<ImageEntity>();
                List<image> images = DB.images.Where(r => r.isInRecycleBin != null && r.isInRecycleBin == true).ToList();

                if (images != null)
                    foreach (var img in images)
                    {
                        rec.Add(Casting.ImageCast.GetImageEntity(img));
                    }
                return new WebResult<List<ImageEntity>>()
                {
                    Status = true,
                    Message = "Ok",
                    Value = rec
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
        public static WebResult<bool> HasGroom()
        {
            try
            {
                if (DB.Grooms.FirstOrDefault(f => f.name == "groom.jpg") == null)
                    return new WebResult<bool>()
                    {
                        Status=true,
                        Message="Ok",
                        Value=false
                    };
                return new WebResult<bool>()
                {
                    Status = true,
                    Message = "Ok",
                    Value = true
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
    }

}