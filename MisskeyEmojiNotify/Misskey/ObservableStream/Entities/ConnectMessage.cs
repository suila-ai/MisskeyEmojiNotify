using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.ObservableStream.Entities
{
    internal class ConnectMessage : IMessageEntity<ConnectMessage.BodyEntity>
    {
        public string Type { get; } = "connect";
        public BodyEntity Body { get; }

        public ConnectMessage(string id, string channel)
        {
            Body = new() { Id = id, Channel = channel };
        }

        internal class BodyEntity : IMessageEntity<BodyEntity>.IBody
        {
            public string Id { get; init; } = "";
            public string Channel { get; init; } = "";
        }
    }
}
