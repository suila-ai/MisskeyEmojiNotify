using MisskeySharp;
using MisskeySharp.Streaming;
using MisskeySharp.Streaming.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
