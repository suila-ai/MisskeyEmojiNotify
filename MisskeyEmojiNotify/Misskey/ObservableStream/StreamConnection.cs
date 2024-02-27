using MisskeySharp;
using MisskeySharp.Streaming;
using System.Reactive.Linq;
using Websocket.Client;

namespace MisskeyEmojiNotify.Misskey.ObservableStream
{
    internal class StreamConnection : IDisposable
    {
        private readonly WebsocketClient client;

        public StreamConnection(MisskeyService misskey)
        {
            var uri = new UriBuilder(misskey.Host)
            {
                Scheme = misskey.Host.StartsWith("https") ? "wss" : "ws",
                Path = "/streaming",
                Query = $"i={misskey.AccessToken}"
            }.Uri;

            client = new(uri)
            {
                ReconnectTimeout = null,
                ErrorReconnectTimeout = TimeSpan.FromSeconds(5),
            };

            client.DisconnectionHappened.Subscribe(e =>
                Console.Error.WriteLine($"{nameof(StreamConnection)}: Disconnected due to {e.Type}")
            );
        }

        public Task<StreamChannel<T>> Connect<T>(MisskeyStreamingChannels channel, string? type = null)
        {
            var id = Guid.NewGuid().ToString();
            var channelName = channel.ToString();
            channelName = char.ToLowerInvariant(channelName[0]) + channelName[1..];

            var instance = StreamChannel<T>.Connect(client, id, channelName, type);
            return instance;
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
