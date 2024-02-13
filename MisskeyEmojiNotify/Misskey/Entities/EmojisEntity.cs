using MisskeyEmojiNotify.Misskey.Entities;
using MisskeySharp;

namespace MisskeyEmojiNotify.Misskey
{
    internal class EmojisEntity : MisskeyApiEntitiesBase
    {
        public IReadOnlyList<Emoji> Emojis { get; init; } = Array.Empty<Emoji>();
    }
}
