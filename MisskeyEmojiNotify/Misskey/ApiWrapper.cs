using MisskeyEmojiNotify.Misskey.Entities;
using MisskeyEmojiNotify.Misskey.ObservableStream;
using MisskeySharp;
using MisskeySharp.Entities;
using MisskeySharp.Streaming;
using System.Reactive.Linq;

using Note = MisskeyEmojiNotify.Misskey.Entities.Note;
using User = MisskeyEmojiNotify.Misskey.Entities.User;

namespace MisskeyEmojiNotify.Misskey
{
    internal class ApiWrapper
    {
        private static readonly VoidParameter voidParameter = new();

        private readonly MisskeyService misskey;
        private readonly StreamConnection streamConnection;
        private bool isOldVersion;

        public static async Task<ApiWrapper> Create()
        {
            var misskey = new MisskeyService(EnvVar.MisskeyServer);
            await misskey.AuthorizeWithAccessTokenAsync(EnvVar.MisskeyToken);

            var instance = new ApiWrapper(misskey);
            await instance.CheckVersion();

            return instance;
        }

        private ApiWrapper(MisskeyService misskey)
        {
            this.misskey = misskey;
            streamConnection = new(misskey);
        }

        private async Task CheckVersion()
        {
            try
            {
                var meta = await misskey.PostAsync<MetaParams, Meta>("meta", new() { Detail = false });
                var majerVersion = meta.Version.Split('.')[0];
                if (int.TryParse(majerVersion, out var version))
                {
                    if (version < 13) isOldVersion = true;

                    Console.Error.WriteLine($"{nameof(CheckVersion)}: {version}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(CheckVersion)}: {ex}");
            }
        }

        public async Task<IReadOnlyList<Emoji>?> GetEmojis()
        {
            if (isOldVersion)
            {
                try
                {
                    var meta = await misskey.PostAsync<MetaParams, Meta>("meta", new() { Detail = false });

                    Console.Error.WriteLine($"{nameof(GetEmojis)}: Found {meta.Emojis?.Count}");

                    return meta.Emojis;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{nameof(GetEmojis)}: {ex}");
                }
            }
            else
            {
                try
                {
                    var emojis = await misskey.PostAsync<VoidParameter, EmojisEntity>("emojis", voidParameter);

                    Console.Error.WriteLine($"{nameof(GetEmojis)}: Found {emojis.Emojis.Count}");

                    return emojis.Emojis;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{nameof(GetEmojis)}: {ex}");
                }
            }

            return null;
        }

        public async Task<bool> Post(string text)
        {
            try
            {
                await misskey.Notes.Create(new()
                {
                    Text = text,
                    Visibility = EnvVar.Visibility.ToApiString()
                });

                Console.Error.WriteLine($"{nameof(Post)}: {text}");

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(Post)}: {ex}");
            }

            return false;
        }

        public async Task<bool> Reply(Note note, string text)
        {
            var visibility = EnvVar.Visibility;
            if (note.Visibility == NoteVisibility.Specified) visibility = NoteVisibility.Specified;

            try
            {
                await misskey.Notes.Create(new()
                {
                    Text = text,
                    Visibility = visibility.ToApiString(),
                    ReplyId = note.Id,
                });

                Console.Error.WriteLine($"{nameof(Reply)}: {note.Id} <- {text}");

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(Reply)}: {ex}");
            }

            return false;
        }

        public async Task<bool> Reaction(Note note, string emoji)
        {
            try
            {
                await misskey.PostAsync<ReactionParams, VoidResponse>("notes/reactions/create", new()
                {
                    NoteId = note.Id,
                    Reaction = emoji
                });

                Console.Error.WriteLine($"{nameof(Reaction)}: {note.Id} <- {emoji}");

                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(Reaction)}: {ex}");
            }

            return false;
        }

        public async Task<bool> SetBanner(ReadOnlyMemory<byte> image, string type)
        {
            try
            {
                var name = $"banner_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                using var stream = new MemoryStream(image.Length);
                await stream.WriteAsync(image);
                stream.Seek(0, SeekOrigin.Begin);

                var file = await misskey.Drive.Files.Create(new()
                {
                    ContentStream = stream,
                    FileName = name,
                    ContentType = type
                });

                await misskey.PostAsync<ProfileUpdateParams, VoidResponse>("i/update", new()
                {
                    BannerId = file.Id
                });

                Console.Error.WriteLine($"{nameof(SetBanner)}: {name}");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(SetBanner)}: {ex}");
            }

            return false;
        }

        public async Task<IReadOnlyList<Note>?> GetLocalNotesForDay(DateOnly date)
        {
            var result = new List<Note>();

            var since = new DateTimeOffset(date, TimeOnly.MinValue, TimeZoneInfo.Local.BaseUtcOffset);
            var until = since.AddDays(1);

            try
            {
                var notes = await misskey.PostAsync<TimelineParams, Notes>("notes/local-timeline", new()
                {
                    Limit = 100,
                    UntilDate = until.ToUnixTimeMilliseconds()
                });

                while (notes[^1].CreatedAt >= since)
                {
                    result.AddRange(notes);

                    notes = await misskey.PostAsync<TimelineParams, Notes>("notes/local-timeline", new()
                    {
                        Limit = 100,
                        UntilId = notes[^1].Id
                    });
                }

                result.AddRange(notes.Where(e => e.CreatedAt >= since));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(GetLocalNotesForDay)}: {ex}");
                return null;
            }

            return result;
        }

        public async Task<User?> GetUser(string id)
        {
            try
            {
                var user = await misskey.PostAsync<UserParams, User>("users/show", new() { UserId = id });
                return user;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"{nameof(GetUser)}: {ex}");
            }

            return null;
        }

        public Task<StreamChannel<Note>> GetMentions()
        {
            return streamConnection.Connect<Note>(MisskeyStreamingChannels.Main, "mention");
        }
    }
}
