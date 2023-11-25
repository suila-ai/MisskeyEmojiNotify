using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey
{
    internal record Emoji(
        string Name,
        string? Category,
        ISet<string> Aliases,
        string Url
    );
}
