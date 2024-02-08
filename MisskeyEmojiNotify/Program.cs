namespace MisskeyEmojiNotify
{
    internal class Program
    {
        static async Task Main(string[] _)
        {
            var jobRunner = await JobRunner.Create();
            if (jobRunner == null) return;
            var dailyJobRunner = new DailyJobRunner(jobRunner);
            var mentionHandler = new MentionHandler(jobRunner);

            try
            {
                await Task.WhenAll(
                    jobRunner.Run(),
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