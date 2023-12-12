using MisskeyEmojiNotify.Misskey.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MisskeyEmojiNotify.Misskey
{
    internal static class EmojiExtensions
    {
        private static readonly HttpClient httpClient = new();

        public static string GetImagePath(this Emoji emoji)
        {
            var path = Path.Combine(EnvVar.ImageDir, HttpUtility.UrlEncode(emoji.Url));
            return path;
        }

        public static async Task<bool> EqualsRemote(this Emoji emoji, bool save = false)
        {
            using var sha1 = SHA1.Create();

            var res = await httpClient.GetByteArrayAsync(emoji.Url);
            var remotehash = sha1.ComputeHash(res);

            sha1.Initialize();

            var path = emoji.GetImagePath();
            using var file = File.Open(path, FileMode.OpenOrCreate);
            var localHash = await sha1.ComputeHashAsync(file);

            var result = remotehash.SequenceEqual(localHash);

            if (save && !result)
            {
                file.SetLength(0);
                await file.WriteAsync(res);
            }

            return result;
        }
    }
}
