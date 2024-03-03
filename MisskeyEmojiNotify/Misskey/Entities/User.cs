using MisskeySharp;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class User : MisskeyApiEntitiesBase, ISimpleUser
    {
        public string Id { get; init; } = "";
        public string? Host { get; init; }
        public bool IsBot { get; init; }

        public bool IsFollowed { get; init; }
        public bool IsFollowing { get; init; }
    }
}
