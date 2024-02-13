namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal enum NoteVisibility
    {
        Public,
        Home,
        Followers,
        Specified
    }

    internal static class NoteVisibilityExtensions
    {
        public static string ToApiString(this NoteVisibility visibility)
        {
            return visibility switch
            {
                NoteVisibility.Public => "public",
                NoteVisibility.Home => "home",
                NoteVisibility.Followers => "followers",
                NoteVisibility.Specified => "specified",
                _ => throw new ArgumentException(null, nameof(visibility))
            };
        }

        public static NoteVisibility FromApiString(string visibility)
        {
            return visibility switch
            {
                "public" => NoteVisibility.Public,
                "home" => NoteVisibility.Home,
                "followers" => NoteVisibility.Followers,
                "specified" => NoteVisibility.Specified,
                _ => throw new ArgumentException(null, nameof(visibility))
            };
        }
    }
}
