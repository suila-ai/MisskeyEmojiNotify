using MisskeySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey
{
    internal class EmojisEntity : MisskeyApiEntitiesBase
    {
        public IReadOnlyList<Emoji> Emojis { get; init; } = Array.Empty<Emoji>();
    }
}
