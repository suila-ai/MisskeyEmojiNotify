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
                using var res = await httpClient.GetAsync(Url);
                res.EnsureSuccessStatusCode();
                actualUrl = res.RequestMessage?.RequestUri?.ToString();
                Memory<byte> data = await res.Content.ReadAsByteArrayAsync();

                var update = UpdateHash(data.Span) || force;

                if (update)
                {
                    UpdateImageFormat(data.Span);

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

        private bool UpdateHash(ReadOnlySpan<byte> bytes)
        {
            var hash = (stackalloc byte[20]);
            SHA1.TryHashData(bytes, hash, out var _);
            var hashStr = Convert.ToHexString(hash);

            if (ImageHash != hashStr)
            {
                imageHash = hashStr;
                return true;
            }

            return false;
        }

        private void UpdateImageFormat(ReadOnlySpan<byte> image)
        {
            imageFormat = image switch
            {
                [0xFF, 0xD8, ..] => "jpg",
                [0x89, 0x50, 0x4E, 0x47, ..] => "png",
                [0x47, 0x49, 0x46, ..] => "gif",
                [0x42, 0x4D, ..] => "bmp",
                [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..] => "webp",
                [0x3C, 0x3F, 0x78, 0x6D, 0x6C, ..] => "svg",
                _ => "bin"
            };
        }
    }
}
