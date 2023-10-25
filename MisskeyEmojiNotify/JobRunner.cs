﻿using MisskeyEmojiNotify.Misskey;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify
{
    internal class JobRunner
    {
        private readonly ApiWrapper apiWrapper;
        private EmojiStore emojiStore;


        public static async Task<JobRunner?> Create()
        {
            var apiWrapper = await ApiWrapper.Create();
            if (apiWrapper == null) return null;

            var emojiStore = await EmojiStore.LoadArchive();
            if (emojiStore == null)
            {
                var emojis = await apiWrapper.GetEmojis();
                if (emojis == null) return null;

                emojiStore = new EmojiStore(emojis);
                var result = await emojiStore.SaveArchive();
                if (!result) return null;
            }

            var instance = new JobRunner(apiWrapper, emojiStore);
            return instance;
        }

        private JobRunner(ApiWrapper apiWrapper, EmojiStore emojiStore)
        {
            this.apiWrapper = apiWrapper;
            this.emojiStore = emojiStore;
        }

        public async Task Run()
        {
            while (true)
            {
                var timer = Task.Delay(EnvVar.CheckInterval);

                var newEmojis = await apiWrapper.GetEmojis();
                if (newEmojis == null)
                {
                    await timer;
                    continue;
                }

                var addList = new List<Emoji>();
                var nameChanges = new List<Change>();
                var categoryChanges = new List<Change>();
                var aliasChanges = new List<Change>();
                var imageChanges = new List<Change>();

                var undeleted = new HashSet<Emoji>();

                foreach (var emoji in newEmojis)
                {
                    var old = emojiStore.GetByName(emoji.Name);
                    if (old != null)
                    {
                        var change = new Change(old, emoji);
                        if (emoji.Name != old.Name) nameChanges.Add(change);
                        if (emoji.Category != old.Category) categoryChanges.Add(change);
                        if (!emoji.Aliases.SetEquals(old.Aliases)) aliasChanges.Add(change);
                        if (emoji.Url != old.Url) imageChanges.Add(change);
                    }
                    else
                    {
                        old = emojiStore.GetByImage(emoji.Url);
                        if (old != null)
                        {
                            nameChanges.Add(new(old, emoji));
                        }
                        else
                        {
                            addList.Add(emoji);
                        }
                    }

                    if (old != null) undeleted.Add(old);
                }

                var deleteList = emojiStore.Except(undeleted).ToList();

                await PostAddEmojis(addList);
                await PostDeleteEmojis(deleteList);
                await PostNameChangeEmojis(nameChanges);
                await PostCategoryChangeEmojis(categoryChanges);
                await PostAliasChangeEmojis(aliasChanges);
                await PostImageChangeEmojis(imageChanges);

                emojiStore = new(newEmojis);
                await emojiStore.SaveArchive();

                await timer;
            }
        }

        private async Task PostAddEmojis(List<Emoji> emojis)
        {
            if (emojis.Count == 0) return;
            var text = "【絵文字追加】\n" + string.Join("\n\n", emojis.Select(e =>
                $"$[x2 :{e.Name}:] ({e.Name})\n" +
                $"カテゴリ: {e.Category ?? "(なし)"}\n" +
                $"タグ: {string.Join(' ', e.Aliases)}\n" +
                $"?[画像]({TrickUrl(e.Url)})"));

            await apiWrapper.Post(text);
        }

        private async Task PostDeleteEmojis(List<Emoji> emojis)
        {
            if (emojis.Count == 0) return;
            var text = "【絵文字削除】\n" + string.Join("\n\n", emojis.Select(e => $"{e.Name}\n?[旧画像]({TrickUrl(e.Url)})"));

            await apiWrapper.Post(text);
        }

        private async Task PostNameChangeEmojis(List<Change> changes)
        {
            if (changes.Count == 0) return;

            var text = "【名前変更】\n" + string.Join("\n\n", changes.Select(e => $"$[x2 :{e.New.Name}:]\n({e.Old.Name} → {e.New.Name})"));
            await apiWrapper.Post(text);
        }

        private async Task PostCategoryChangeEmojis(List<Change> changes)
        {
            foreach (var group in changes.GroupBy(e => (oldCategory: e.Old.Category, newCategory: e.New.Category)))
            {
                var text = "【カテゴリ変更】\n" +
                    $"{group.Key.oldCategory ?? "(なし)"} → {group.Key.newCategory ?? "(なし)"}\n" +
                    string.Join(' ', group.Select(e => $":{e.New.Name}:"));
                await apiWrapper.Post(text);
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

            await apiWrapper.Post(text);
        }

        private async Task PostImageChangeEmojis(List<Change> changes)
        {
            if (changes.Count == 0) return;

            var text = "【画像変更】\n" + string.Join("\n\n", changes.Select(e => $"$[x2 :{e.New.Name}:] ({e.New.Name})\n?[旧画像]({TrickUrl(e.Old.Url)}) ?[新画像]({TrickUrl(e.New.Url)})"));

            await apiWrapper.Post(text);
        }

        private string TrickUrl(string url)
        {
            if (url.StartsWith(EnvVar.MisskeyServer))
            {
                var replaced = Regex.Replace(url, "^(https?://)", "$1/");
                return replaced;
            }

            return url;
        }

        private record Change(Emoji Old, Emoji New);
    }
}
