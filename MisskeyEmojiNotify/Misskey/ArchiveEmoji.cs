using ImageMagick;
using MisskeyEmojiNotify.Misskey.Entities;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace MisskeyEmojiNotify.Misskey
{
    class ArchiveEmoji : Emoji
    {
        [JsonPropertyOrder(1000)]
        public string? ActualUrl { get => actualUrl; init => actualUrl = value; }
        [JsonPropertyOrder(1000)]
        public string? ImageHash { get => imageHash; init => imageHash = value; }
        [JsonPropertyOrder(1000)]
        public string? ImageFormat { get => imageFormat; init => imageFormat = value; }

        [JsonIgnore]
        public string? ImagePath => ImageHash != null && ImageFormat != null ? Path.Combine(EnvVar.ImageDir, $"{ImageHash}.{ImageFormat}") : null;

        private string? actualUrl = null;
        private string? imageHash = null;
        private string? imageFormat = null;

        private static readonly HttpClient httpClient = new();

        public ArchiveEmoji() { }

        public ArchiveEmoji(Emoji emoji)
        {
            Name = emoji.Name;
            Category = emoji.Category;
            Aliases = emoji.Aliases;
            Url = emoji.Url;
        }


        public async Task<bool> SyncRemote(bool force = false)
        {
            try
            {
                using var sha1 = SHA1.Create();

                var res = await httpClient.GetAsync(Url);
                res.EnsureSuccessStatusCode();
                actualUrl = res.RequestMessage?.RequestUri?.ToString();
                var data = await res.Content.ReadAsByteArrayAsync();
                var remotehash = sha1.ComputeHash(data);

                sha1.Initialize();

                var update = true;

                if (!force && ImageHash != null)
                {
                    var localHash = Convert.FromHexString(ImageHash);
                    update = !remotehash.SequenceEqual(localHash);
                }

                if (update)
                {
                    imageHash = Convert.ToHexString(remotehash);

                    using var image = new MagickImage(data);
                    imageFormat = image.Format.ToString().ToLowerInvariant();

                    if (ImagePath == null) return false;
                    if (!Directory.Exists(EnvVar.ImageDir)) Directory.CreateDirectory(EnvVar.ImageDir);
                    using var file = File.Open(ImagePath, FileMode.Create);
                    await file.WriteAsync(data);
                }

                return update;
            }
            catch
            {
                return false;
            }
        }
    }
}
