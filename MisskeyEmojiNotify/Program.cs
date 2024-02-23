using MisskeyEmojiNotify.Misskey;

namespace MisskeyEmojiNotify
{
    internal class Program
    {
        static async Task Main(string[] _)
        {
            var apiWrapper = await ApiWrapper.Create();
            if (apiWrapper == null) return;

            var intervalJobRunner = await IntervalJobRunner.Create(apiWrapper);
            var dailyJobRunner = new DailyJobRunner(apiWrapper);
            var mentionHandler = new MentionHandler(apiWrapper);

            if (intervalJobRunner == null) return;

            try
            {
                await Task.WhenAll(
                    intervalJobRunner.Run(),
                    dailyJobRunner.Run(),
                    mentionHandler.Start()
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"UNHANDLED: {ex}");
            }
        }
    }
}