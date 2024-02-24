namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class SimpleUser : ISimpleUser
    {
        public string Id { get; init; } = "";
        public string? Host { get; init; }
        public bool IsBot { get; init; }
    }
}
