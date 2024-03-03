using MisskeyEmojiNotify.Misskey;
using MisskeyEmojiNotify.Misskey.Entities;
using MisskeyEmojiNotify.SsvParser;
using System.Reactive.Linq;

namespace MisskeyEmojiNotify
{
    internal class MentionHandler(ApiWrapper apiWrapper)
    {
        private readonly ApiWrapper apiWrapper = apiWrapper;
        private readonly Random random = new();

        public async Task Start()
        {
            var mentions = await apiWrapper.GetMentions();

            await mentions.Where(e => !e.User.IsBot).Select(note => Observable.FromAsync(async () =>
            {
                if (EnvVar.RequireFollowed == RequireFollowed.All || (EnvVar.RequireFollowed == RequireFollowed.Remote && note.User.Host != null))
                {
                    var user = await apiWrapper.GetUser(note.User.Id);
                    if (user?.IsFollowed != true) return;
                }

                var results = await Task.WhenAll(
                    GachaHander(note)
                );

                var notHandled = results.All(e => e == null);
                if (notHandled) await apiWrapper.Reaction(note, "❓");

                var failed = results.Any(e => e == false);
                if (failed) await apiWrapper.Reaction(note, "⚠️");

            })).Concat();
        }

        public async Task<bool?> GachaHander(Note note)
        {
            var text = $"{note.Cw ?? ""}\n{note.Text ?? ""}";

            var fields = Ssv.Parse(text).SkipWhile(e => e is not ("ガチャ" or "gacha")).ToArray();

            if (fields.Length == 0) return null;

            var count = 1;
            var emojis = new List<Emoji>();

            var emojiStore = await EmojiStore.GetInstance();
            if (emojiStore == null) return false;

            if (fields is [_, var countStr, .. var categories])
            {
                if (!int.TryParse(countStr, out count)) return null;
                count = Math.Min(count, 50);

                if (categories.Length > 0)
                {
                    var categorySet = new HashSet<string>(categories);
                    emojis.AddRange(emojiStore.Where(e => e.Category != null && categorySet.Contains(e.Category)));
                }
            }

            if (emojis.Count == 0) emojis.AddRange(emojiStore);

            var result = string.Join("", Enumerable.Range(0, count).Select(_ => emojis[random.Next(emojis.Count)]).Select(e => $":{e.Name}:"));
            await apiWrapper.Reply(note, result);

            return true;
        }
    }
}
