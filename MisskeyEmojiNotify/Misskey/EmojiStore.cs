using MisskeyEmojiNotify.Misskey.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MisskeyEmojiNotify.Misskey
{
    internal class EmojiStore : IReadOnlyCollection<Emoji>
    {
        private readonly Dictionary<string, Emoji> emojisByName = [];
        private readonly Dictionary<string, Emoji> emojisByImage = [];

        private readonly HttpClient httpClient = new();

        public int Count => emojisByName.Values.Count;

        public static async Task<EmojiStore?> LoadArchive()
        {
            try
            {
                using var stream = File.Open(EnvVar.ArchiveFile, FileMode.Open);
                var archive = await JsonSerializer.DeserializeAsync<IReadOnlyList<Emoji>>(stream);
                if (archive != null) return new(archive);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(LoadArchive)}: {ex}");
            }

            return null;
        }

        public EmojiStore(IEnumerable<Emoji> emojis)
        {
            foreach (var emoji in emojis)
            {
                emojisByName.Add(emoji.Name, emoji);
                emojisByImage.Add(emoji.Url, emoji);
            }
        }

        public Emoji? GetByName(string name)
        {
            emojisByName.TryGetValue(name, out Emoji? result);
            return result;
        }

        public Emoji? GetByImage(string url)
        {
            emojisByImage.TryGetValue(url, out Emoji? result);
            return result;
        }

        public async Task<bool> SaveArchive()
        {
            try
            {
                using var stream = File.Open(EnvVar.ArchiveFile, FileMode.Create);
                await JsonSerializer.SerializeAsync(stream, emojisByName.Values);

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(SaveArchive)}: {ex}");
            }

            return false;
        }

        public async Task<int> SaveImages(bool force = false)
        {
            DirectoryInfo dir;
            try
            {
                dir = Directory.CreateDirectory(EnvVar.ImageDir);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(SaveImages)}: {ex}");
                return 0;
            }

            List<string> urls;
            if (force)
            {
                urls = [.. emojisByImage.Keys];
            }
            else
            {
                urls = emojisByImage.Keys.Except(dir.EnumerateFiles().Select(e => HttpUtility.UrlDecode(e.Name))).ToList();
            }

            var count = 0;

            foreach (var url in urls)
            {
                var path = Path.Combine(dir.FullName, HttpUtility.UrlEncode(url));
                try
                {
                    var res = await httpClient.GetStreamAsync(url);
                    using var file = File.Open(path, FileMode.Create);
                    await res.CopyToAsync(file);

                    count++;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{nameof(SaveImages)}: {ex}");

                    try
                    {
                        File.Delete(path);
                    }
                    catch { }
                }
            }

            return count;
        }

        public static string GetImagePath(Emoji emoji)
        {
            var path = Path.Combine(EnvVar.ImageDir, HttpUtility.UrlEncode(emoji.Url));
            return path;
        }

        public IEnumerator<Emoji> GetEnumerator() => emojisByName.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
