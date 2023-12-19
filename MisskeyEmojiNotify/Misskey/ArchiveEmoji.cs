using ImageMagick;
using MisskeyEmojiNotify.Misskey.Entities;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace MisskeyEmojiNotify.Misskey
{
    class ArchiveEmoji : Emoji
    {
        [JsonPropertyOrder(1000)]
        public string? ImageHash { get => imageHash; init => imageHash = value; }
        [JsonPropertyOrder(1001)]
        public string? ImageFormat { get => imageFormat; init => imageFormat = value; }

        [JsonIgnore]
        public string? ImagePath => ImageHash != null && ImageFormat != null ? Path.Combine(EnvVar.ImageDir, $"{ImageHash}.{ImageFormat}") : null;

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

                var res = await httpClient.GetByteArrayAsync(Url);
                var remotehash = sha1.ComputeHash(res);

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

                    using var image = new MagickImage(res);
                    imageFormat = image.Format.ToString().ToLowerInvariant();

                    if (ImagePath == null) return false;
                    using var file = File.Open(ImagePath, FileMode.Create);
                    await file.WriteAsync(res);
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
