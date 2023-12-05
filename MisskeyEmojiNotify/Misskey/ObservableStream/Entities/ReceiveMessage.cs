using MisskeyEmojiNotify.Misskey.ObservableStream.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
