using MisskeySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey
{
    internal class ApiWrapper
    {
        private static readonly MisskeyApiEntitiesBase emptyEntity = new();

        private readonly MisskeyService misskey;
        private bool isOldVersion;

        public static async Task<ApiWrapper?> Create()
        {
            var instance = new ApiWrapper(new(EnvVar.MisskeyServer));

            await instance.misskey.AuthorizeWithAccessTokenAsync(EnvVar.MisskeyToken);
            await instance.CheckVersion();

            return instance;
        }

        private ApiWrapper(MisskeyService misskey)
        {
            this.misskey = misskey;
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
                    var emojis = await misskey.PostAsync<MisskeyApiEntitiesBase, EmojisEntity>("emojis", emptyEntity);

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
                    Visibility = EnvVar.Visibility
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
    }
}
