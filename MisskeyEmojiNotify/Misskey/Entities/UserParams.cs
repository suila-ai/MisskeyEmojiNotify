﻿using MisskeySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.Misskey.Entities
{
    internal class UserParams : MisskeyApiEntitiesBase
    {
        public string? UserId { get; init; }
    }
}
