using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify
{
    internal class DailyJobRunner(JobRunner jobRunner)
    {
        private readonly JobRunner jobRunner = jobRunner;

        public async Task Run()
        {
            var lastRun = DateOnly.FromDateTime(DateTime.Now);

            while (true)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);

                if (lastRun < today)
                {
                    var yesterday = today.AddDays(-1);

                    var notes = await jobRunner.ApiWrapper.GetLocalNotesForDay(yesterday);
                    var reactions = notes.SelectMany(e => e.Reactions)
                        .Where(e => Regexes.StandardOrLocalEmoji().IsMatch(e.Key))
                        .GroupBy(e => e.Key)
                        .Select(e => (emoji: e.Key.Replace("@.", ""), count: e.Sum(e => e.Value)))
                        .ToArray();
                    var reactionsCount = reactions.Sum(e => e.count);

                    var rankedIn = reactions.OrderByDescending(e => e.count).Take(10).GroupBy(e => e.count).SelectMany((e, i) => e.Select(e => (e.emoji, e.count, rank: i))).ToArray();

                    var text = $"【昨日({yesterday:MM/dd})のリアクション】\n" +
                        string.Join("", rankedIn.Select(e => RankingFormat(e.rank, e.emoji, e.count))) + "\n\n" +
                        $"ノート数: {notes.Count} リアクション数: {reactionsCount}";

                    await jobRunner.ApiWrapper.Post(text);

                    lastRun = today;
                }

                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        private static string RankingFormat(int rank, string emoji, int count)
        {
            if (rank < 0 || rank > 9) return string.Empty;

            ReadOnlySpan<string> rankEmojis = ["🥇", "🥈", "🥉", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟"];
            var text = $"{rankEmojis[rank]} {emoji}";

            var mfmText = rank switch
            {
                0 => $"$[x2 {text}] x{count}\n",
                2 => $"{text} x{count}\n",
                _ => $"{text} x{count}  ",
            };

            return mfmText;
        }
    }
}
