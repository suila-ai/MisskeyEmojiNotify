using MisskeyEmojiNotify.Misskey.ObservableStream.Entities;

namespace MisskeyEmojiNotify.Misskey.ObservableStream
{
    internal class ReceiveMessage<T> : IMessageEntity<ReceiveMessage<T>.BodyEntity>
    {
        public string Type { get; init; } = "";
        public BodyEntity Body { get; init; } = new BodyEntity();

        internal class BodyEntity : IMessageEntity<BodyEntity>.IBody
        {
            public string Id { get; init; } = "";
            public string Type { get; init; } = "";
            public T Body { get; init; } = default!;
        }
    }
}
