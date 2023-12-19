using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class Emoji {
        public string Name { get; init; } = "";
        public string? Category { get; init;} = null;
        public ISet<string> Aliases { get; init; } = new HashSet<string>();
        public string Url { get; init; } = "";
    }
}
