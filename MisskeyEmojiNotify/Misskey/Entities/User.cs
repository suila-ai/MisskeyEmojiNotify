using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class User
    {
        public string Id { get; init; } = "";
        public bool IsBot { get; init; }
    }
}
