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
        private readonly Dictionary<string, Emoji> emojisByName = new();
        private readonly Dictionary<string, Emoji> emojisByImage = new();

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

        public async Task<bool> SaveImages(bool force = false)
        {
            DirectoryInfo dir;
            try
            {
                dir = Directory.CreateDirectory(EnvVar.ImageDir);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(SaveImages)}: {ex}");
                return false;
            }

            List<string> urls;
            if (force)
            {
                urls = emojisByImage.Keys.ToList();
            }
            else
            {
                urls = emojisByImage.Keys.Except(dir.EnumerateFiles().Select(e => HttpUtility.UrlDecode(e.Name))).ToList();
            }

            var succeeded = true;

            foreach (var url in urls)
            {
                var path = Path.Combine(dir.FullName, HttpUtility.UrlEncode(url));
                try
                {
                    var res = await httpClient.GetStreamAsync(url);
                    using var file = File.Open(path, FileMode.Create);
                    await res.CopyToAsync(file);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{nameof(SaveImages)}: {ex}");

                    try
                    {
                        File.Delete(path);
                    }
                    catch { }

                    succeeded = false;
                }
            }

            return succeeded;
        }

        public IEnumerator<Emoji> GetEnumerator() => emojisByName.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
