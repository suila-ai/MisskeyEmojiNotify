using MisskeyEmojiNotify.Misskey.Entities;
using System.Globalization;

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

        public static IReadOnlySet<Function> DisableFunctions { get; } = GetEnvVar("MISSKEY_DISABLE_FUNCTIONS", [],
            str => str.Split(',').Select(e => {
                if (Enum.TryParse<Function>(e.Trim(), true, out var result)) return result;
                return Function.None;
            }).Where(e => e != Function.None).ToHashSet()
        );

        public static RequireFollowed RequireFollowed { get; } = GetEnvVar("MISSKEY_REQUIRE_FOLLOWED", RequireFollowed.None, str => Enum.Parse<RequireFollowed>(str, true));

        public static IReadOnlySet<string> FortuneCategories { get; } = GetEnvVar("MISSKEY_FORTUNE_CATEGORIES", [], str => str.Split('\n').ToHashSet());

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
            if (envVar == null) return defaultValue;

            try
            {
                return parser(envVar);
            }
            catch
            {
                return defaultValue;
            }
        }

        internal class MissingEnvVarException(string? message) : Exception(message)
        {
        }
    }
}
