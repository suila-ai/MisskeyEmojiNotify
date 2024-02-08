using System.Text.RegularExpressions;

namespace MisskeyEmojiNotify
{
    internal static partial class Regexes
    {
        [GeneratedRegex("^(https?://)")]
        public static partial Regex HttpProto();

        [GeneratedRegex(@"^(:.+@\.:)|([^:]+)$")]
        public static partial Regex StandardOrLocalEmoji();
    }
}