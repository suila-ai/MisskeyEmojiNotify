namespace MisskeyEmojiNotify.SsvParser
{
    internal interface IState
    {
        public int Index { get; }

        public IState Next(char c);
    }

    internal interface IFieldState : IState
    {
        public int Start { get; }
        public int End { get; }
    }
}
