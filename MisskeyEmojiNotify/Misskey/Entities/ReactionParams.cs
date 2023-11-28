using MisskeySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class ReactionParams : MisskeyApiEntitiesBase
    {
        public string NoteId { get; set; } = "";
        public string Reaction { get; set; } = "";
    }
}
