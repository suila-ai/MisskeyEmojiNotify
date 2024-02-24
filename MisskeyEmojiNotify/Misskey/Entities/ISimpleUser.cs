namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal interface ISimpleUser
    {
        string Id { get; init; }
        string? Host { get; init; }
        bool IsBot { get; init; }
    }
}
