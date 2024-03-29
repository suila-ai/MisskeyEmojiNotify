﻿using MisskeyEmojiNotify.Misskey;

namespace MisskeyEmojiNotify
{
    internal class DailyJobRunner(ApiWrapper apiWrapper)
    {
        private readonly ApiWrapper apiWrapper = apiWrapper;

        public async Task Run()
        {
            var lastRun = DateOnly.FromDateTime(DateTime.Now);

            while (true)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);

                if (lastRun < today)
                {
                    if (!EnvVar.DisableFunctions.Contains(Function.DailyRanking))
                    {
                        await AggregateRanking(today);
                    }

                    lastRun = today;
                }

                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        private async Task AggregateRanking(DateOnly today)
        {
            var yesterday = today.AddDays(-1);

            var notes = await apiWrapper.GetLocalNotesForDay(yesterday);

            var retryCount = 0;
            while (notes == null)
            {
                if (retryCount > 10) return;
                retryCount++;
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));

                notes = await apiWrapper.GetLocalNotesForDay(yesterday);
            }

            var reactions = notes.SelectMany(e => e.Reactions)
                .Where(e => Regexes.StandardOrLocalEmoji().IsMatch(e.Key))
                .GroupBy(e => e.Key)
                .Select(e => new RankedEmojis([e.Key.Replace("@.", "")], e.Sum(e => e.Value), -1))
                .ToArray();
            var reactionsCount = reactions.Sum(e => e.Count);

            var rankedIn = reactions.OrderByDescending(e => e.Count)
                .Select((e, i) => e with { Rank = i })
                .GroupBy(e => e.Count)
                .TakeWhile(e => e.Any(e => e.Rank < 10))
                .Select(e => new RankedEmojis([.. e.SelectMany(e => e.Emojis)], e.Key, e.Min(e => e.Rank)))
                .ToArray();

            var text = $"【昨日({yesterday:MM/dd})のリアクション】\n" +
                string.Join("\n",
                    rankedIn
                        .GroupBy(e => e.Rank switch { 0 => 0, < 3 => 1, _ => 2 })
                        .Select(e => string.Join("  ", e.Select(e => e.Format())))
                ) + "\n\n" +
                $"ノート数: {notes.Count} リアクション数: {reactionsCount}";

            await apiWrapper.Post(text);
        }

        private record RankedEmojis(IReadOnlyList<string> Emojis, int Count, int Rank)
        {
            public string Format()
            {
                if (Rank is < 0 or > 9) return string.Empty;

                ReadOnlySpan<string> rankEmojis = ["🥇", "🥈", "🥉", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟"];
                var text = $"{rankEmojis[Rank]} {string.Join("", Emojis)}";

                var mfmText = Rank switch
                {
                    0 => $"$[x2 {text}] x{Count}",
                    _ => $"{text} x{Count}",
                };

                return mfmText;
            }
        }
    }
}
