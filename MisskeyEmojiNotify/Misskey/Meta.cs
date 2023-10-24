using MisskeySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey
{
    internal class Meta : MisskeyApiEntitiesBase
    {
        public string Version { get; init; } = "";
        public IReadOnlyList<Emoji>? Emojis { get; init; }
    }
}
