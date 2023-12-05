using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MisskeyEmojiNotify.SsvParser
{
    internal class SeparatedState(int index) : IState
    {
        public int Index { get; private set; } = index;

        public IState Next(char c)
        {
            Index++;
            if (c == '\0' || char.IsWhiteSpace(c)) return this;
            if (c is '"' or '\'') return new QuoteStartFieldState(Index, c, Index, Index);
            return new FieldState(Index);
        }
    }

    internal class FieldState(int index) : IFieldState
    {
        public int Index { get; private set; } = index;
        public int Start { get; private set; } = index;
        public int End { get; private set; } = index;

        public IState Next(char c)
        {
            Index++;
            if (c == '\0' || char.IsWhiteSpace(c)) return new SeparatedState(Index);
            End++;
            return this;
        }
    }

    internal class QuoteStartFieldState(int index, char quote, int start, int end) : IFieldState
    {
        public int Index { get; private set; } = index;
        public int Start { get; private set; } = start;
        public int End { get; private set; } = end;

        private readonly char quote = quote;

        public IState Next(char c)
        {
            Index++;
            if (c == '\0') return new SeparatedState(Index);
            if (c == quote) return new QuoteEndFieldState(Index, quote, Start + 1, End);
            End++;
            return this;
        }
    }

    internal class QuoteEndFieldState(int index, char quote, int start, int end) : IFieldState
    {
        public int Index { get; private set; } = index;
        public int Start { get; private set; } = start;
        public int End { get; private set; } = end;

        private readonly char quote = quote;

        public IState Next(char c)
        {
            Index++;
            if (c == '\0' || char.IsWhiteSpace(c)) return new SeparatedState(Index);
            return new QuoteStartFieldState(Index, quote, Start - 1, End + 2);
        }
    }
}
