using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify
{
    internal class EnvVar
    {
        public static string MisskeyServer { get; } = GetEnvVar("MISSKEY_SERVER");
        public static string MisskeyToken { get; } = GetEnvVar("MISSKEY_TOKEN");

        public static TimeSpan CheckInterval { get; } = TimeSpan.FromSeconds(int.Parse(GetEnvVar("MISSKEY_CHECK_INTERVAL", "600")));
        public static string Visibility { get; } = GetEnvVar("MISSKEY_VISIBILITY", "specified");

        public static string ArchiveFile { get; } = GetEnvVar("MISSKEY_ARCHIVE_FILE", "./archive.json");

        private static string GetEnvVar(string name, string? defaultValue = null)
        {
            var envVar = Environment.GetEnvironmentVariable(name);
            if (envVar != null) return envVar;
            if (defaultValue != null) return defaultValue;
            throw new MissingEnvVarException($"\"{name}\" is required");
        }

        internal class MissingEnvVarException : Exception
        {
            public MissingEnvVarException(string? message) : base(message)
            {
            }
        }
    }
}
