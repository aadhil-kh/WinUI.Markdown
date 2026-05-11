using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinUI.Markdown.Themes;

namespace WinUI.Markdown.Visitors;

internal sealed class NativeSyntaxHighlighter
{
    private static readonly HashSet<string> CStyleKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "async", "await", "base", "bool", "break", "case", "catch", "class", "const",
        "continue", "default", "delegate", "do", "double", "else", "enum", "event", "false", "finally",
        "float", "for", "foreach", "if", "in", "int", "interface", "internal", "is", "namespace", "new",
        "null", "object", "out", "override", "private", "protected", "public", "readonly", "return",
        "sealed", "static", "string", "struct", "switch", "this", "throw", "true", "try", "using",
        "var", "virtual", "void", "while"
    };

    private readonly Brush _plain;
    private readonly Brush _keyword = Solid("#FF8957E5");
    private readonly Brush _string = Solid("#FF0A3069");
    private readonly Brush _comment = Solid("#FF6E7781");
    private readonly Brush _number = Solid("#FF0550AE");
    private readonly Brush _property = Solid("#FF953800");

    public NativeSyntaxHighlighter(MarkdownTheme theme)
    {
        _plain = theme.CodeForeground;
    }

    public void Append(InlineCollection target, string code, string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            Add(target, code, _plain);
            return;
        }

        var normalized = language.Trim().ToLowerInvariant();
        if (normalized is "json")
        {
            AppendJson(target, code);
            return;
        }

        if (normalized is "xml" or "html" or "xaml")
        {
            AppendMarkup(target, code);
            return;
        }

        AppendCStyle(target, code);
    }

    private void AppendCStyle(InlineCollection target, string code)
    {
        for (var i = 0; i < code.Length;)
        {
            if (StartsWith(code, i, "//"))
            {
                var end = IndexOfLineEnd(code, i);
                Add(target, code[i..end], _comment);
                i = end;
            }
            else if (code[i] is '"' or '\'')
            {
                var end = ReadQuoted(code, i);
                Add(target, code[i..end], _string);
                i = end;
            }
            else if (char.IsDigit(code[i]))
            {
                var start = i++;
                while (i < code.Length && (char.IsDigit(code[i]) || code[i] == '.'))
                {
                    i++;
                }

                Add(target, code[start..i], _number);
            }
            else if (IsIdentifierStart(code[i]))
            {
                var start = i++;
                while (i < code.Length && IsIdentifierPart(code[i]))
                {
                    i++;
                }

                var word = code[start..i];
                Add(target, word, CStyleKeywords.Contains(word) ? _keyword : _plain);
            }
            else
            {
                Add(target, code[i++].ToString(), _plain);
            }
        }
    }

    private void AppendJson(InlineCollection target, string code)
    {
        for (var i = 0; i < code.Length;)
        {
            if (code[i] == '"')
            {
                var end = ReadQuoted(code, i);
                var next = SkipWhitespace(code, end);
                Add(target, code[i..end], next < code.Length && code[next] == ':' ? _property : _string);
                i = end;
            }
            else if (char.IsDigit(code[i]) || code[i] == '-')
            {
                var start = i++;
                while (i < code.Length && (char.IsDigit(code[i]) || code[i] is '.' or 'e' or 'E' or '+' or '-'))
                {
                    i++;
                }

                Add(target, code[start..i], _number);
            }
            else
            {
                Add(target, code[i++].ToString(), _plain);
            }
        }
    }

    private void AppendMarkup(InlineCollection target, string code)
    {
        for (var i = 0; i < code.Length;)
        {
            if (StartsWith(code, i, "<!--"))
            {
                var end = code.IndexOf("-->", i, StringComparison.Ordinal);
                end = end < 0 ? code.Length : end + 3;
                Add(target, code[i..end], _comment);
                i = end;
            }
            else if (code[i] == '<' || code[i] == '>')
            {
                Add(target, code[i++].ToString(), _keyword);
            }
            else if (code[i] is '"' or '\'')
            {
                var end = ReadQuoted(code, i);
                Add(target, code[i..end], _string);
                i = end;
            }
            else
            {
                Add(target, code[i++].ToString(), _plain);
            }
        }
    }

    private static int SkipWhitespace(string text, int start)
    {
        while (start < text.Length && char.IsWhiteSpace(text[start]))
        {
            start++;
        }

        return start;
    }

    private static int ReadQuoted(string text, int start)
    {
        var quote = text[start++];
        while (start < text.Length)
        {
            if (text[start] == '\\')
            {
                start += 2;
                continue;
            }

            if (text[start++] == quote)
            {
                break;
            }
        }

        return Math.Min(start, text.Length);
    }

    private static int IndexOfLineEnd(string text, int start)
    {
        var end = text.IndexOf('\n', start);
        return end < 0 ? text.Length : end;
    }

    private static bool StartsWith(string text, int start, string value)
    {
        return start + value.Length <= text.Length && text.AsSpan(start, value.Length).SequenceEqual(value);
    }

    private static bool IsIdentifierStart(char value)
    {
        return char.IsLetter(value) || value == '_';
    }

    private static bool IsIdentifierPart(char value)
    {
        return char.IsLetterOrDigit(value) || value == '_';
    }

    private static void Add(InlineCollection target, string text, Brush brush)
    {
        target.Add(new Run { Text = text, Foreground = brush });
    }

    private static SolidColorBrush Solid(string value)
    {
        var parsed = uint.Parse(value.TrimStart('#'), global::System.Globalization.NumberStyles.HexNumber);
        return new SolidColorBrush(Color.FromArgb(
            (byte)(parsed >> 24),
            (byte)(parsed >> 16),
            (byte)(parsed >> 8),
            (byte)parsed));
    }
}
