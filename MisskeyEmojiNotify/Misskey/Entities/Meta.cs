using MisskeyEmojiNotify.Misskey.Entities;
using MisskeySharp;

namespace MisskeyEmojiNotify.Misskey
{
    internal class Meta : MisskeyApiEntitiesBase
    {
        public string Version { get; init; } = "";
        public IReadOnlyList<Emoji>? Emojis { get; init; }
    }
}
