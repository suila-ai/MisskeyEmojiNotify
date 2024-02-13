namespace MisskeyEmojiNotify.Misskey.ObservableStream.Entities
{
    internal class DisconnectMessage : IMessageEntity<DisconnectMessage.BodyEntity>
    {
        public string Type { get; } = "disconnect";
        public BodyEntity Body { get; }

        public DisconnectMessage(string id)
        {
            Body = new() { Id = id };
        }

        internal class BodyEntity : IMessageEntity<BodyEntity>.IBody
        {
            public string Id { get; init; } = "";
        }
    }
}
