using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using MisskeyEmojiNotify.Misskey.ObservableStream.Entities;
using Websocket.Client;

namespace MisskeyEmojiNotify.Misskey.ObservableStream
{
    internal class StreamChannel<T> : IObservable<T>, IDisposable
    {
        private readonly WebsocketClient client;
        private readonly string id;
        private readonly IObservable<T> observable;

        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private StreamChannel(WebsocketClient client, string id, IObservable<T> observable)
        {
            this.client = client;
            this.id = id;
            this.observable = observable;
        }

        internal static async Task<StreamChannel<T>> Connect(WebsocketClient client, string id, string channel, string? type = null)
        {
            var connectMessage = JsonSerializer.Serialize(new ConnectMessage(id, channel), jsonSerializerOptions);

            var observable = client.MessageReceived.Select(e =>
            {
                try
                {
                    if (e.Text == null) return null;
                    var data = JsonSerializer.Deserialize<ReceiveMessage<T>>(e.Text, jsonSerializerOptions);
                    return data?.Body;
                }
                catch { return null; }
            }).Where(e => e != null && e.Id == id && e.Type == type).Select(e => e!.Body);

            client.DisconnectionHappened.Subscribe(_ => { client.Send(connectMessage); });

            if (!client.IsStarted) await client.Start();
            if (!client.IsRunning) await client.Reconnect();

            client.Send(connectMessage);

            var instance = new StreamChannel<T>(client, id, observable);

            return instance;
        }


        public void Disconnect()
        {
            var message = JsonSerializer.Serialize(new DisconnectMessage(id));
            client.Send(message);
        }

        public IDisposable Subscribe(IObserver<T> observer) => observable.Subscribe(observer);

        public void Dispose() => Disconnect();
    }
}
