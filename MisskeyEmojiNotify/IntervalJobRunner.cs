using ImageMagick;
using MisskeyEmojiNotify.Misskey;
using MisskeyEmojiNotify.Misskey.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify
{
    internal class IntervalJobRunner
    {
        public ApiWrapper ApiWrapper { get; }
        public EmojiStore EmojiStore { get; private set; }

        public static async Task<IntervalJobRunner?> Create()
        {
            var apiWrapper = await ApiWrapper.Create();
            if (apiWrapper == null) return null;

            var emojiStore = await EmojiStore.LoadArchive();
            if (emojiStore == null)
            {
                var emojis = await apiWrapper.GetEmojis();
                if (emojis == null) return null;

                emojiStore = new EmojiStore(emojis);
                await emojiStore.SaveImages(true);
                var result = await emojiStore.SaveArchive();
                if (!result) return null;
            }

            var instance = new IntervalJobRunner(apiWrapper, emojiStore);
            return instance;
        }

        private IntervalJobRunner(ApiWrapper apiWrapper, EmojiStore emojiStore)
        {
            ApiWrapper = apiWrapper;
            EmojiStore = emojiStore;
        }

        public async Task Run()
        {
            while (true)
            {
                var timer = Task.Delay(EnvVar.CheckInterval);

                var newEmojis = await ApiWrapper.GetEmojis();
                if (newEmojis == null)
                {
                    await timer;
                    continue;
                }

                var newEmojiStore = new EmojiStore(newEmojis);
                await newEmojiStore.SaveImages();

                await CheckDifference(newEmojiStore);

                EmojiStore = newEmojiStore;
                await EmojiStore.SaveArchive();

                await timer;
            }
        }

        private async Task CheckDifference(EmojiStore newEmojiStore)
        {
            var addList = new List<ArchiveEmoji>();
            var nameChanges = new List<Change>();
            var imageChanges = new List<Change>();
            var categoryChanges = new List<Change>();
            var aliasChanges = new List<Change>();

            var undeleted = new HashSet<ArchiveEmoji>();

            foreach (var emoji in newEmojiStore)
            {
                var old = EmojiStore.GetByName(emoji.Name);
                if (old != null)
                {
                    var change = new Change(old, emoji);
                    if (emoji.Name != old.Name) nameChanges.Add(change);
                    if (emoji.ImageHash != null && old.ImageHash != null && emoji.ImageHash != old.ImageHash) imageChanges.Add(change);
                    if (emoji.Category != old.Category) categoryChanges.Add(change);
                    if (!emoji.Aliases.SetEquals(old.Aliases)) aliasChanges.Add(change);
                }
                else
                {
                    old = EmojiStore.GetByImage(emoji.Url);

                    if (old != null) nameChanges.Add(new(old, emoji));
                    else addList.Add(emoji);
                }

                if (old != null) undeleted.Add(old);
            }

            var deleteList = EmojiStore.Except(undeleted).ToList();

            await PostAddEmojis(addList);
            await PostDeleteEmojis(deleteList);
            await PostNameChangeEmojis(nameChanges);
            await PostImageChangeEmojis(imageChanges);
            await PostCategoryChangeEmojis(categoryChanges);
            await PostAliasChangeEmojis(aliasChanges);

            if (addList.Count + imageChanges.Count + deleteList.Count > 0) await UpdateBanner();
        }

        private async Task PostAddEmojis(List<ArchiveEmoji> emojis)
        {
            if (emojis.Count == 0) return;
            var text = "【絵文字追加】\n" + string.Join("\n\n", emojis.Select(e =>
                $"$[x2 :{e.Name}:] ({e.Name})\n" +
                $"カテゴリ: {e.Category ?? "(なし)"}\n" +
                $"タグ: {(e.Aliases.Count > 0 ? string.Join(' ', e.Aliases) : "(なし)")}\n" +
                $"{ImageLink("画像", e)}"));

            await ApiWrapper.Post(text);
        }

        private async Task PostDeleteEmojis(List<ArchiveEmoji> emojis)
        {
            if (emojis.Count == 0) return;
            var text = "【絵文字削除】\n" + string.Join("\n\n", emojis.Select(e => $"{e.Name}\n{ImageLink("旧画像", e)}"));

            await ApiWrapper.Post(text);
        }

        private async Task PostNameChangeEmojis(List<Change> changes)
        {
            if (changes.Count == 0) return;

            var text = "【名前変更】\n" + string.Join("\n\n", changes.Select(e => $"$[x2 :{e.New.Name}:]\n({e.Old.Name} → {e.New.Name})"));
            await ApiWrapper.Post(text);
        }

        private async Task PostCategoryChangeEmojis(List<Change> changes)
        {
            foreach (var group in changes.GroupBy(e => (oldCategory: e.Old.Category, newCategory: e.New.Category)))
            {
                var text = "【カテゴリ変更】\n" +
                    $"{group.Key.oldCategory ?? "(なし)"} → {group.Key.newCategory ?? "(なし)"}\n" +
                    string.Join(' ', group.Select(e => $":{e.New.Name}:"));
                await ApiWrapper.Post(text);
            }
        }

        private async Task PostAliasChangeEmojis(List<Change> changes)
        {
            if (changes.Count == 0) return;

            var text = "【タグ変更】\n" + string.Join("\n", changes.Select(e =>
            (
                name: e.New.Name,
                add: string.Join(' ', e.New.Aliases.Except(e.Old.Aliases)),
                delete: string.Join(' ', e.Old.Aliases.Except(e.New.Aliases))

            )).Where(e => e.add.Length > 0 || e.delete.Length > 0).Select(e =>
            {
                var text = $"$[x2 :{e.name}:] ({e.name})\n";
                if (e.add.Length > 0) text += $"追加: {e.add}\n";
                if (e.delete.Length > 0) text += $"削除: {e.delete}\n";

                return text;
            }));

            await ApiWrapper.Post(text);
        }

        private async Task PostImageChangeEmojis(List<Change> changes)
        {
            if (changes.Count == 0) return;

            var text = "【画像変更】\n" + string.Join("\n\n", changes.Select(e =>
                $"$[x2 :{e.New.Name}:] ({e.New.Name})\n" +
                $"{((e.Old.ActualUrl ?? e.Old.Url) != (e.New.ActualUrl ?? e.New.Url) ? $"{ImageLink("旧画像", e.Old)} " : "")}{ImageLink("新画像", e.New)}"
            ));

            await ApiWrapper.Post(text);
        }

        private async Task UpdateBanner()
        {
            var montage = await CreateMontage();
            await ApiWrapper.SetBanner(montage, "image/png");
        }

        private Task<ReadOnlyMemory<byte>> CreateMontage()
        {
            return Task.Run(() =>
            {
                using var images = new MagickImageCollection();

                var tile = CalcTileGeometry(EmojiStore.Count, 2.0);
                var size = new MagickGeometry(1100, 0);
                var tileSize = new MagickGeometry(size.Width / tile.Width);

                foreach (var emoji in EmojiStore)
                {
                    if (emoji.ImagePath == null) continue;

                    var image = new MagickImage(emoji.ImagePath);
                    image.Resize(tileSize);
                    images.Add(image);
                }

                var margin = (int)(tileSize.Width * 0.05);
                using var montage = images.Montage(new MontageSettings()
                {
                    TileGeometry = tile,
                    Geometry = new MagickGeometry(margin, margin, 0, 0)
                });

                montage.Resize(size);
                montage.Format = MagickFormat.Png;

                ReadOnlyMemory<byte> memory = montage.ToByteArray();
                return memory;
            });
        }


        private static MagickGeometry CalcTileGeometry(int count, double ratio)
        {
            var x = Math.Sqrt(count * ratio);

            var xFloor = (int)Math.Floor(x);
            var y1 = (int)Math.Ceiling((double)count / xFloor);

            var xCeil = (int)Math.Ceiling(x);
            var y2 = (int)Math.Ceiling((double)count / xCeil);

            var compare = (xFloor * y1).CompareTo(xCeil * y2);


            if (compare < 0) return new(xFloor, y1);
            if (compare > 0) return new(xCeil, y2);

            if (Math.Abs((double)xFloor / y1 - ratio) < Math.Abs((double)xCeil / y2 - ratio))
            {
                return new(xFloor, y1);
            }
            else
            {
                return new(xCeil, y2);
            }
        }

        private static string ImageLink(string text, ArchiveEmoji emoji)
        {
            return $"?[{text}]({TrickUrl(emoji.ActualUrl ?? emoji.Url)})";
        }

        private static string TrickUrl(string url)
        {
            if (url.StartsWith(EnvVar.MisskeyServer))
            {
                var replaced = Regexes.HttpProto().Replace(url, "$1/");
                return replaced;
            }

            return url;
        }

        private record Change(ArchiveEmoji Old, ArchiveEmoji New);
    }
}
