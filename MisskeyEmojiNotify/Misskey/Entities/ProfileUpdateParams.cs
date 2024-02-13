using MisskeySharp;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class ProfileUpdateParams : MisskeyApiEntitiesBase
    {
        public string? BannerId { get; init; } = null;
    }
}
