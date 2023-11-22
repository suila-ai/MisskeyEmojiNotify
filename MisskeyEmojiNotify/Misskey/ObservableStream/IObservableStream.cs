using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.ObservableStream
{
    internal interface IObservableStream<T> : IObservable<T>, IDisposable
    {
    }
}
