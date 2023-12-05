using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class Note
    {
        public string Id { get; init; } = "";
        public string? Text { get; init; } = null;
        public string? Cw { get; init; } = null;
        public User User { get; init; } = new();
        public NoteVisibility Visibility { get; init; }
    }
}
