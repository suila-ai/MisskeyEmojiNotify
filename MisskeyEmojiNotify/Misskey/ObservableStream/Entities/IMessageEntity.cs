namespace MisskeyEmojiNotify.Misskey.ObservableStream.Entities
{
    internal interface IMessageEntity<T> where T : IMessageEntity<T>.IBody
    {
        public string Type { get; }
        public T Body { get; }

        interface IBody
        {
            string Id { get; }
        }
    }
}
