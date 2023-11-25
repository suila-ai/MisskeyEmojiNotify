using MisskeyEmojiNotify.Misskey.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify
{
    internal class EnvVar
    {
        public static string MisskeyServer { get; } = GetEnvVar("MISSKEY_SERVER");
        public static string MisskeyToken { get; } = GetEnvVar("MISSKEY_TOKEN");

        public static TimeSpan CheckInterval { get; } = TimeSpan.FromSeconds(GetEnvVar("MISSKEY_CHECK_INTERVAL", 600.0));
        public static NoteVisibility Visibility { get; } = GetEnvVar("MISSKEY_VISIBILITY", NoteVisibility.Specified, NoteVisibilityExtensions.FromApiString);

        public static string ArchiveFile { get; } = GetEnvVar("MISSKEY_ARCHIVE_FILE", "./archive.json");
        public static string ImageDir { get; } = GetEnvVar("MISSKEY_IMAGE_DIR", "./images");

        private static string GetEnvVar(string name, string? defaultValue = null)
        {
            var envVar = Environment.GetEnvironmentVariable(name);
            if (envVar != null) return envVar;
            if (defaultValue != null) return defaultValue;
            throw new MissingEnvVarException($"\"{name}\" is required");
        }

        private static T GetEnvVar<T>(string name, T defaultValue) where T : IParsable<T>
        {
            var envVar = Environment.GetEnvironmentVariable(name);
            if (envVar != null && T.TryParse(envVar, CultureInfo.InvariantCulture, out var result)) return result;
            return defaultValue;
        }

        private static T GetEnvVar<T>(string name, T defaultValue, Func<string, T> parser)
        {
            var envVar = Environment.GetEnvironmentVariable(name);
            if (envVar != null) return parser(envVar);
            return defaultValue;
        }

        internal class MissingEnvVarException : Exception
        {
            public MissingEnvVarException(string? message) : base(message)
            {
            }
        }
    }
}
