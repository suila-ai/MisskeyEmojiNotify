namespace MisskeyEmojiNotify.SsvParser
{
    internal static class Ssv
    {
        public static IEnumerable<string> Parse(string text)
        {
            IState state = new SeparatedState(-1);

            foreach (var c in text.Append('\0'))
            {
                var nextState = state.Next(c);

                if (state is IFieldState fieldState && nextState is SeparatedState)
                {
                    yield return text[fieldState.Start..(fieldState.End + 1)];
                }

                state = nextState;
            }
        }
    }
}
