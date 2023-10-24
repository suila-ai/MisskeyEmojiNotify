namespace MisskeyEmojiNotify
{
    internal class Program
    {
        static async Task Main(string[] _)
        {
            var jobRunner = await JobRunner.Create();
            if (jobRunner == null) return;

            await jobRunner.Run();
        }
    }
}