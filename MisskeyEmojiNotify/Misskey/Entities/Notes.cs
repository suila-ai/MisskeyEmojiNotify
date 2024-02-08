using MisskeySharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class Notes : MisskeyApiEntitiesBase, IList<Note>
    {
        private readonly List<Note> notes = [];

        public Note this[int index] { get => notes[index]; set => notes[index] = value; }

        public int Count => notes.Count;

        public bool IsReadOnly => ((ICollection<Note>)notes).IsReadOnly;

        public void Add(Note item) => notes.Add(item);

        public void Clear() => notes.Clear();

        public bool Contains(Note item) => notes.Contains(item);

        public void CopyTo(Note[] array, int arrayIndex) => notes.CopyTo(array, arrayIndex);

        public IEnumerator<Note> GetEnumerator() => notes.GetEnumerator();

        public int IndexOf(Note item) => notes.IndexOf(item);

        public void Insert(int index, Note item) => notes.Insert(index, item);

        public bool Remove(Note item) => notes.Remove(item);

        public void RemoveAt(int index) => notes.RemoveAt(index);

        IEnumerator IEnumerable.GetEnumerator() => notes.GetEnumerator();
    }
}
