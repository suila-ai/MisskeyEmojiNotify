using MisskeyEmojiNotify.Misskey.Entities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MisskeyEmojiNotify.Misskey
{
    internal class EmojiStore : IReadOnlyCollection<ArchiveEmoji>
    {
        private readonly Dictionary<string, ArchiveEmoji> emojisByName = [];
        private readonly Dictionary<string, ArchiveEmoji> emojisByImage = [];

        private readonly HttpClient httpClient = new();

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public int Count => emojisByName.Values.Count;

        public static async Task<EmojiStore?> LoadArchive()
        {
            try
            {
                using var stream = File.Open(EnvVar.ArchiveFile, FileMode.Open);
                var archive = await JsonSerializer.DeserializeAsync<IReadOnlyList<ArchiveEmoji>>(stream, jsonSerializerOptions);
                if (archive != null) return new(archive);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(LoadArchive)}: {ex}");
            }

            return null;
        }

        public EmojiStore(IEnumerable<ArchiveEmoji> emojis)
        {
            foreach (var emoji in emojis)
            {
                emojisByName.Add(emoji.Name, emoji);
                emojisByImage.Add(emoji.Url, emoji);
            }
        }

        public EmojiStore(IEnumerable<Emoji> emojis) : this(emojis.Select(emoji => new ArchiveEmoji(emoji))) { }

        public ArchiveEmoji? GetByName(string name)
        {
            emojisByName.TryGetValue(name, out ArchiveEmoji? result);
            return result;
        }

        public ArchiveEmoji? GetByImage(string url)
        {
            emojisByImage.TryGetValue(url, out ArchiveEmoji? result);
            return result;
        }

        public async Task<bool> SaveArchive()
        {
            try
            {
                using var stream = File.Open(EnvVar.ArchiveFile, FileMode.Create);
                await JsonSerializer.SerializeAsync(stream, emojisByName.Values, jsonSerializerOptions);

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
            var count = 0;

            foreach (var emoji in emojisByName.Values)
            {
                var result = await emoji.SyncRemote(force);
                if (result) count++;
            }

            return count;
        }

        public IEnumerator<ArchiveEmoji> GetEnumerator() => emojisByName.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
