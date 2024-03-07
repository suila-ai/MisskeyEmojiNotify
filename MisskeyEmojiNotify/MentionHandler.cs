using MisskeyEmojiNotify.Misskey;
using MisskeyEmojiNotify.Misskey.Entities;
using MisskeyEmojiNotify.SsvParser;
using System.Reactive.Linq;

namespace MisskeyEmojiNotify
{
    internal class MentionHandler(ApiWrapper apiWrapper)
    {
        private readonly ApiWrapper apiWrapper = apiWrapper;

        public async Task Start()
        {
            var mentions = await apiWrapper.GetMentions();

            await mentions.Where(e => !e.User.IsBot).Select(async note =>
            {
                if (EnvVar.RequireFollowed == RequireFollowed.All || (EnvVar.RequireFollowed == RequireFollowed.Remote && note.User.Host != null))
                {
                    var user = await apiWrapper.GetUser(note.User.Id);
                    if (user?.IsFollowed != true) return;
                }

                var result = await HandleAll(note, [
                    GachaHander,
                    FortuneHandler,
                ]);

                var reaction = result switch
                {
                    true => null,
                    false => "⚠️",
                    null => "❓",
                };

                if (reaction != null) await apiWrapper.Reaction(note, reaction);
            });
        }

        private static async Task<bool?> HandleAll(Note note, IReadOnlyList<Func<Note, ValueTask<bool?>>> handlers)
        {
            foreach (var handler in handlers)
            {
                var result = await handler(note);
                if (result != null) return result;
            }

            return null;
        }

        private async ValueTask<bool?> GachaHander(Note note)
        {
            if (EnvVar.DisableFunctions.Contains(Function.Gacha)) return null;

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

            var random = new Random();
            var result = string.Join("", Enumerable.Range(0, count).Select(_ => emojis[random.Next(emojis.Count)]).Select(e => $":{e.Name}:"));
            return await apiWrapper.Reply(note, result);
        }

        private async ValueTask<bool?> FortuneHandler(Note note)
        {
            if (EnvVar.DisableFunctions.Contains(Function.Fortune)) return null;

            IReadOnlyList<string> keywords = ["占", "うらな", "運勢", "おみくじ"];

            if (note.Text == null || !keywords.Any(note.Text.Contains)) return null;

            IReadOnlyList<Emoji> emojis = [.. await EmojiStore.GetInstance()];
            if (emojis == null) return false;

            var fortuneEmojis = EnvVar.FortuneCategories switch
            {
                { Count: 0 } => emojis,
                _ => [.. emojis.Where(e => e.Category != null && EnvVar.FortuneCategories.Contains(e.Category))],
            };

            if (fortuneEmojis.Count == 0) return false;

            var random = new Random($"{DateTime.Now:yyyyMMdd}{note.User.Id}".GetHashCode());

            var fortuneEmoji = fortuneEmojis[random.Next(fortuneEmojis.Count)];
            var fortuneSuffix = random.Next(3) switch
            {
                < 2 => "吉",
                _ => "凶",
            };
            var luckeyEmoji = emojis[random.Next(emojis.Count)];

            var text = "今日のあなたの運勢は\n" +
                $":{fortuneEmoji.Name}:{fortuneSuffix}\n" +
                $"幸運の絵文字: :{luckeyEmoji.Name}:";

            return await apiWrapper.Reply(note, text);
        }
    }
}
