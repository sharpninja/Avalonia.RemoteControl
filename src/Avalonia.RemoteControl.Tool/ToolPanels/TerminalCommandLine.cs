using System.Text;

namespace Avalonia.RemoteControl.Tool;

internal static class TerminalCommandLine
{
    public static string[] ParseArguments(string? arguments)
    {
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return [];
        }

        var parsed = new List<string>();
        var current = new StringBuilder();
        char? quote = null;

        for (var index = 0; index < arguments.Length; index++)
        {
            var character = arguments[index];
            if (character == '\\' && index + 1 < arguments.Length && IsQuote(arguments[index + 1]))
            {
                current.Append(arguments[index + 1]);
                index++;
                continue;
            }

            if (IsQuote(character))
            {
                if (quote == character)
                {
                    quote = null;
                    continue;
                }

                if (quote is null)
                {
                    quote = character;
                    continue;
                }
            }

            if (char.IsWhiteSpace(character) && quote is null)
            {
                FlushCurrent();
                continue;
            }

            current.Append(character);
        }

        FlushCurrent();
        return [.. parsed];

        void FlushCurrent()
        {
            if (current.Length == 0)
            {
                return;
            }

            parsed.Add(current.ToString());
            current.Clear();
        }

        static bool IsQuote(char character)
        {
            return character is '"' or '\'';
        }
    }
}
