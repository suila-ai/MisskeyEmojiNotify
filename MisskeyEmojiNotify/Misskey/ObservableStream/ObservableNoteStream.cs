using MisskeySharp;
using MisskeySharp.Entities;
using MisskeySharp.Streaming;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.ObservableStream
{
    internal class ObservableNoteStream : IObservableStream<Note>
    {
        private readonly MisskeyService misskey;
        private readonly MisskeyStreamingConnection connection;
        private readonly string? type;

        private readonly List<IObserver<Note>> observers = new();

        public ObservableNoteStream(MisskeyService misskey, MisskeyStreamingChannels channel, string? type = null)
        {
            this.misskey = misskey;
            connection = misskey.Streaming.Connect(channel);
            this.type = type;

            misskey.Streaming.NoteReceived += Handler;
            misskey.Streaming.ConnectionClosed += ClosedHandler;
        }

        private void Handler(object? sender, MisskeyNoteReceivedEventArgs args)
        {
            if (args.NoteMessage.Body.Id == connection.Id && (type == null || args.NoteMessage.Body.Type == type))
            {
                foreach (var observer in observers)
                {
                    observer.OnNext(args.NoteMessage.Body.Body);
                }
            }
        }

        private void ClosedHandler(object? sender, EventArgs args)
        {
            foreach (var observer in observers)
            {
                observer.OnError(new MisskeyException("Streaming connection closed."));
                observer.OnCompleted();
            }
        }

        public IDisposable Subscribe(IObserver<Note> observer)
        {
            observers.Add(observer);
            return Disposable.Create(() => observers.Remove(observer));
        }

        public void Dispose()
        {
            misskey.Streaming.NoteReceived -= Handler;
            misskey.Streaming.ConnectionClosed -= ClosedHandler;

            misskey.Streaming.Disconnect(connection);
        }
    }
}
