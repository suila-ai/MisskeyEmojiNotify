namespace MisskeyEmojiNotify
{
    internal class DailyJobRunner(IntervalJobRunner jobRunner)
    {
        private readonly IntervalJobRunner jobRunner = jobRunner;

        public async Task Run()
        {
            var lastRun = DateOnly.FromDateTime(DateTime.Now);

            while (true)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);

                if (lastRun < today)
                {
                    await AggregateRanking(today);

                    lastRun = today;
                }

                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        private async Task AggregateRanking(DateOnly today)
        {
            var yesterday = today.AddDays(-1);

            var notes = await jobRunner.ApiWrapper.GetLocalNotesForDay(yesterday);
            var reactions = notes.SelectMany(e => e.Reactions)
                .Where(e => Regexes.StandardOrLocalEmoji().IsMatch(e.Key))
                .GroupBy(e => e.Key)
                .Select(e => new RankedEmoji(e.Key.Replace("@.", ""), e.Sum(e => e.Value), -1))
                .ToArray();
            var reactionsCount = reactions.Sum(e => e.Count);

            var rankedIn = reactions.OrderByDescending(e => e.Count)
                .Select((e, i) => e with { Rank = i })
                .GroupBy(e => e.Count)
                .TakeWhile(e => e.Any(e => e.Rank < 10))
                .SelectMany((e, i) => e.Select(e => e with { Rank = i }))
                .ToArray();

            var text = $"【昨日({yesterday:MM/dd})のリアクション】\n" +
                string.Join("", rankedIn.Select(e => e.Format())) + "\n\n" +
                $"ノート数: {notes.Count} リアクション数: {reactionsCount}";

            await jobRunner.ApiWrapper.Post(text);
        }

        private record RankedEmoji(string Emoji, int Count, int Rank)
        {
            public string Format()
            {
                if (Rank is < 0 or > 9) return string.Empty;

                ReadOnlySpan<string> rankEmojis = ["🥇", "🥈", "🥉", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟"];
                var text = $"{rankEmojis[Rank]} {Emoji}";

                var mfmText = Rank switch
                {
                    0 => $"$[x2 {text}] x{Count}\n",
                    2 => $"{text} x{Count}\n",
                    _ => $"{text} x{Count}  ",
                };

                return mfmText;
            }
        }
    }
}
