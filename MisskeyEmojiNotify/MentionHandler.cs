using MisskeyEmojiNotify.Misskey.Entities;
using MisskeyEmojiNotify.SsvParser;
using System.Reactive.Linq;

namespace MisskeyEmojiNotify
{
    internal class MentionHandler(IntervalJobRunner jobRunner)
    {
        private readonly IntervalJobRunner jobRunner = jobRunner;
        private readonly Random random = new();

        public async Task Start()
        {
            var mentions = await jobRunner.ApiWrapper.GetMentions();

            await mentions.Where(e => !e.User.IsBot).Select(note => Observable.FromAsync(async () =>
            {
                var results = await Task.WhenAll(
                    GachaHander(note)
                );

                if (!results.Any(e => e)) await jobRunner.ApiWrapper.Reaction(note, "❓");

            })).Concat();
        }

        public async Task<bool> GachaHander(Note note)
        {
            var text = $"{note.Cw ?? ""}\n{note.Text ?? ""}";

            var fields = Ssv.Parse(text).SkipWhile(e => e is not ("ガチャ" or "gacha")).ToArray();

            if (fields.Length == 0) return false;

            int count = 1;
            var emojis = new List<Emoji>();

            if (fields is [_, var countStr, .. var categories])
            {
                if (!int.TryParse(countStr, out count)) return false;
                count = Math.Min(count, 50);

                if (categories.Length > 0)
                {
                    var categorySet = new HashSet<string>(categories);
                    emojis.AddRange(jobRunner.EmojiStore.Where(e => e.Category != null && categorySet.Contains(e.Category)));
                }
            }

            if (emojis.Count == 0) emojis.AddRange(jobRunner.EmojiStore);

            var result = string.Join("", Enumerable.Range(0, count).Select(_ => emojis[random.Next(emojis.Count)]).Select(e => $":{e.Name}:"));
            await jobRunner.ApiWrapper.Reply(note, result);

            return true;
        }
    }
}
