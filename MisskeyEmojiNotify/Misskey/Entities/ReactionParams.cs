using MisskeySharp;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class ReactionParams : MisskeyApiEntitiesBase
    {
        public string NoteId { get; set; } = "";
        public string Reaction { get; set; } = "";
    }
}
