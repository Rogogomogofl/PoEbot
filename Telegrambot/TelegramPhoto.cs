using Bot;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace Telegrambot
{
    public class TelegramPhoto : AbstractPhoto
    {
        private readonly TelegramBotClient _botClient;
        private Task _photoUploader;

        public TelegramPhoto(string cachePath, long id, TelegramBotClient botClient) : base(cachePath, id)
        {
            _botClient = botClient;
        }

        public override bool GetPresetPhoto(string name)
        {
            switch (name)
            {
                case "delve":
                {
                    _content = new[] {"AgADAgADr6wxGxVgSEs6sklltPi6ZAOUwg8ABAEAAwIAA3kAA_pSAQABFgQ"};
                    return true;
                }
                case "incursion":
                {
                    _content = new[] {"AgADAgADsKwxGxVgSEu-Fwh4MWJZolzPuQ8ABAEAAwIAA3cAA91WBgABFgQ"};
                    return true;
                }
                case "betrayal":
                {
                    _content = new[] {"AgADAgADtawxGxVgSEu2WI3Xvp2ohJPYuQ8ABAEAAwIAA3cAA3NNBgABFgQ"};
                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        public override string[] GetContent()
        {
            if (_content == null)
            {
                _photoUploader?.RunSynchronously();
            }

            return (string[]) _content?.Clone();
        }

        public override bool LoadPhotoFromFile(string name)
        {
            var item = name.ToLower().Replace(' ', '-').Replace("'", "");
            var lines = File.ReadAllLines(CachePath);
            foreach (var line in lines)
            {
                var data = line.Split(' ');
                if (data[0] == item)
                {
                    _content = new[] {data[1]};
                    return true;
                }
            }

            return false;
        }

        public override bool SavePhoto(string name, byte[] bytes)
        {
            _photoUploader = new Task(() =>
            {
                try
                {
                    using (var stream = new MemoryStream(bytes))
                    {
                        var returnedMessage = _botClient.SendPhotoAsync(chatId: Id, photo: stream).Result;
                        using (var streamWriter = new StreamWriter(CachePath, true, Encoding.Default))
                        {
                            streamWriter.WriteLine("{0} {1}", name, returnedMessage.Photo.Last().FileId);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                }
            });
            return true;
        }

        public override bool UploadPhoto(byte[] bytes)
        {
            _photoUploader = new Task(() =>
            {
                try
                {
                    using (var stream = new MemoryStream(bytes))
                    {
                        var returnedMessage = _botClient.SendPhotoAsync(chatId: Id, photo: stream).Result;
                        _content = new[] {returnedMessage.Photo.Last().FileId};
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"{DateTime.Now}: {e.Message} at {GetType()}");
                }
            });
            return true;
        }
    }
}