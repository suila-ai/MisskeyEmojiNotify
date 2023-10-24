using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify
{
    internal class EnvVar
    {
        public static string? MisskeyServer { get; } = Environment.GetEnvironmentVariable("MISSKEY_SERVER");
        public static string? MisskeyToken { get; } = Environment.GetEnvironmentVariable("MISSKEY_TOKEN");

        public static TimeSpan CheckInterval { get; } = TimeSpan.FromSeconds(int.Parse(Environment.GetEnvironmentVariable("MISSKEY_CHECK_INTERVAL") ?? "600"));
        public static string Visibility { get; } = Environment.GetEnvironmentVariable("MISSKEY_VISIBILITY") ?? "specified";

        public static string ArchiveFile { get; } = Environment.GetEnvironmentVariable("MISSKEY_ARCHIVE_FILE") ?? "./archive.json";
    }
}
