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
                        .ToDictionary(e => e.Key.Replace("@.", ""), e => e.Sum(e => e.Value))
                        .OrderByDescending(e => e.Value)
                        .ToArray();
                    var reactionsCount = reactions.Sum(e => e.Value);

                    var text = $"【昨日({yesterday:MM/dd})のリアクション】\n" +
                        string.Join("", reactions.Take(10).Select((e, i) => RankingFormat(i, e.Key))) + "\n\n" +
                        $"ノート数: {notes.Count} リアクション数: {reactionsCount}";

                    await jobRunner.ApiWrapper.Post(text);

                    lastRun = yesterday;
                }

                await Task.Delay(TimeSpan.FromSeconds(60));
            }
        }

        private static string RankingFormat(int rank, string emoji)
        {
            if (rank < 0 || rank > 9) return string.Empty;

            ReadOnlySpan<string> rankEmojis = ["🥇", "🥈", "🥉", "4️⃣", "5️⃣", "6️⃣", "7️⃣", "8️⃣", "9️⃣", "🔟"];
            var text = $"{rankEmojis[rank]} {emoji}";

            var mfmText = rank switch
            {
                0 => $"$[x2 {text}]\n",
                2 => $"{text}\n",
                _ => $"{text}  ",
            };

            return mfmText;
        }
    }
}
