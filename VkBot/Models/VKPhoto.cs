using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using BotHandlers.Abstracts;
using BotHandlers.Static;
using VkNet.Abstractions;

namespace VkBot.Models
{
    public class VkPhoto : AbstractPhoto
    {
        private readonly IPhotoCategory vkPhoto;

        public VkPhoto(string cahePath, long id, IPhotoCategory photo) : base(cahePath, id)
        {
            vkPhoto = photo;
        }

        public override bool SavePhoto(string name, byte[] bytes)
        {
            if (bytes == null)
            {
                return false;
            }

            try
            {
                var uploadServer = vkPhoto.GetMessagesUploadServer(Id);
                string result;
                using (var requestContent = new MultipartFormDataContent())
                using (var imageContent = new ByteArrayContent(bytes))
                {
                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                    requestContent.Add(imageContent, "photo", "image.png");
                    using (var httpClient = new HttpClient())
                    {
                        var response = httpClient.PostAsync(uploadServer.UploadUrl, requestContent).Result;
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                }

                var photo = vkPhoto.SaveMessagesPhoto(result);
                using (var stream = new StreamWriter(CachePath, true, Encoding.Default))
                {
                    stream.WriteLine("{0} {1} {2}", name, photo[0].Id, photo[0].OwnerId);
                }

                Content = new[] {photo[0].Id.ToString(), photo[0].OwnerId.ToString()};
                return true;
            }
            catch (Exception ex)
            {
                Common.Logger.LogError(ex);
                return false;
            }
        }

        public override bool LoadPhotoFromFile(string name)
        {
            var item = name.Replace(' ', '-').Replace("'", "");
            var lines = File.ReadAllLines(CachePath);
            foreach (var line in lines)
            {
                var data = line.Split(' ');
                if (data[0].Equals(item, StringComparison.OrdinalIgnoreCase))
                {
                    Content = new[] {data[1], data[2]};
                    return true;
                }
            }

            return false;
        }

        public override bool GetPresetPhoto(string name)
        {
            switch (name)
            {
                case "delve":
                {
                    Content = new[] {"456241317", "37321011"};
                    return true;
                }
                case "incursion":
                {
                    Content = new[] {"456241318", "37321011"};
                    return true;
                }
                case "betrayal":
                {
                    Content = new[] {"457242989", "37321011"};
                    return true;
                }
                /*case "all":
                {
                    return new string[] { "456241319", "37321011" };
                }*/
                default:
                {
                    return false;
                }
            }
        }

        public override string[] GetContent()
        {
            return (string[]) Content?.Clone();
        }

        public override bool UploadPhoto(byte[] bytes)
        {
            if (bytes == null)
            {
                return false;
            }

            try
            {
                var uploadServer = vkPhoto.GetMessagesUploadServer(Id);
                string result;
                using (var requestContent = new MultipartFormDataContent())
                using (var imageContent = new ByteArrayContent(bytes))
                {
                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
                    requestContent.Add(imageContent, "photo", "image.png");
                    using (var httpClient = new HttpClient())
                    {
                        var response = httpClient.PostAsync(uploadServer.UploadUrl, requestContent).Result;
                        result = response.Content.ReadAsStringAsync().Result;
                    }
                }

                var photo = vkPhoto.SaveMessagesPhoto(result);
                Content = new[] {photo[0].Id.ToString(), photo[0].OwnerId.ToString()};
                return true;
            }
            catch (Exception ex)
            {
                Common.Logger.LogError(ex);
                return false;
            }
        }
    }
}