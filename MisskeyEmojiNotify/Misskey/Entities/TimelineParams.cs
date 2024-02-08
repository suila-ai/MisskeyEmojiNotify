using MisskeySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class TimelineParams : MisskeyApiEntitiesBase
    {
        public int Limit { get; init; } = 10;
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? SinceId { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? UntilId { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long SinceDate { get; init; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public long UntilDate { get; init; }
    }
}
