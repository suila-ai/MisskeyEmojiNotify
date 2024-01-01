using MisskeyEmojiNotify.Misskey.Entities;
using MisskeyEmojiNotify.Misskey.ObservableStream;
using MisskeyEmojiNotify.Misskey.ObservableStream.Entities;
using MisskeySharp;
using MisskeySharp.Entities;
using MisskeySharp.Streaming;
using MisskeySharp.Streaming.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

using Note = MisskeyEmojiNotify.Misskey.Entities.Note;

namespace MisskeyEmojiNotify.Misskey
{
    internal class ApiWrapper
    {
        private static readonly VoidParameter voidParameter = new();

        private readonly MisskeyService misskey;
        private readonly StreamConnection streamConnection;
        private bool isOldVersion;

        public static async Task<ApiWrapper?> Create()
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

        public Task<StreamChannel<Note>> GetMentions()
        {
            return streamConnection.Connect<Note>(MisskeyStreamingChannels.Main, "mention");
        }
    }
}
