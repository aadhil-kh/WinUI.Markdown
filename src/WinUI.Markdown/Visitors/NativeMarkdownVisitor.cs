using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Text;
using Windows.UI.Text;
using WinUI.Markdown.Controls;
using WinUI.Markdown.Themes;
using MarkdigBlock = Markdig.Syntax.Block;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;

namespace WinUI.Markdown.Visitors;

public sealed class NativeMarkdownVisitor
{
    private readonly MarkdownTheme _theme;
    private readonly NativeSyntaxHighlighter _syntaxHighlighter;

    public NativeMarkdownVisitor(MarkdownTheme theme)
    {
        _theme = theme;
        _syntaxHighlighter = new NativeSyntaxHighlighter(theme);
    }

    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    public FrameworkElement Render(MarkdownDocument document)
    {
        var root = CreateStackPanel();
        foreach (var block in document)
        {
            root.Children.Add(RenderBlock(block, 0));
        }

        return new Border
        {
            Background = _theme.Background,
            Padding = new Thickness(0),
            Child = root
        };
    }

    private FrameworkElement RenderBlock(MarkdigBlock block, int nestingLevel)
    {
        return block switch
        {
            HeadingBlock heading => RenderHeading(heading),
            ParagraphBlock paragraph => RenderParagraph(paragraph),
            QuoteBlock quote => RenderQuote(quote, nestingLevel),
            ListBlock list => RenderList(list, nestingLevel),
            FencedCodeBlock code => RenderCodeBlock(code),
            CodeBlock code => RenderCodeBlock(code),
            ThematicBreakBlock => RenderHorizontalRule(),
            Table table => RenderTable(table, nestingLevel),
            HtmlBlock html => RenderRawText(html.Lines.ToString()),
            _ => RenderRawText(block.ToString() ?? string.Empty)
        };
    }

    private TextBlock RenderHeading(HeadingBlock heading)
    {
        var sizeIndex = Math.Clamp(heading.Level - 1, 0, _theme.HeadingSizes.Length - 1);
        return new TextBlock
        {
            Text = InlineText(heading.Inline),
            FontFamily = _theme.BodyFont,
            FontSize = _theme.HeadingSizes[sizeIndex],
            FontWeight = HeadingWeight(sizeIndex),
            TextWrapping = TextWrapping.WrapWholeWords,
            Foreground = _theme.Foreground,
            Margin = new Thickness(0, heading.Level == 1 ? 0 : _theme.HeadingSpacingBefore, 0, _theme.HeadingSpacingAfter)
        };
    }

    private RichTextBlock RenderParagraph(ParagraphBlock paragraph, int skipLeadingCharacters = 0)
    {
        var block = CreateRichTextBlock();
        var xamlParagraph = new Paragraph();
        AppendInlines(xamlParagraph.Inlines, paragraph.Inline, skipLeadingCharacters);
        block.Blocks.Add(xamlParagraph);
        return block;
    }

    private Border RenderQuote(QuoteBlock quote, int nestingLevel)
    {
        var panel = CreateStackPanel();
        foreach (var child in quote)
        {
            panel.Children.Add(RenderBlock(child, nestingLevel + 1));
        }

        return new Border
        {
            BorderBrush = _theme.BlockquoteAccent,
            Background = _theme.BlockquoteBackground,
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = _theme.BlockquoteCornerRadius,
            Padding = new Thickness(12, 4, 10, 4),
            Margin = new Thickness(0, 4, 0, _theme.BlockSpacing),
            Child = panel
        };
    }

    private MarkdownListPanel RenderList(ListBlock list, int nestingLevel)
    {
        var panel = new MarkdownListPanel
        {
            IsOrdered = list.IsOrdered,
            StartNumber = int.TryParse(list.OrderedStart, out var start) ? start : 1,
            NestingLevel = nestingLevel,
            Theme = _theme,
            Margin = new Thickness(0, 2, 0, _theme.ListSpacing)
        };

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var itemPanel = CreateStackPanel();
            var taskState = TryGetTaskState(item);
            var skippedTaskPrefix = false;
            foreach (var child in item)
            {
                if (!skippedTaskPrefix && taskState.HasValue && child is ParagraphBlock paragraph)
                {
                    itemPanel.Children.Add(RenderParagraph(paragraph, skipLeadingCharacters: 4));
                    skippedTaskPrefix = true;
                }
                else
                {
                    itemPanel.Children.Add(RenderBlock(child, nestingLevel + 1));
                }
            }

            panel.AddItem(itemPanel, taskState);
        }

        return panel;
    }

    private ScrollViewer RenderCodeBlock(CodeBlock code)
    {
        var text = code.Lines.ToString();
        var block = CreateRichTextBlock();
        var paragraph = new Paragraph();
        _syntaxHighlighter.Append(paragraph.Inlines, text, code is FencedCodeBlock fenced ? fenced.Info : null);
        block.Blocks.Add(paragraph);
        block.FontFamily = _theme.CodeFont;
        block.Foreground = _theme.CodeForeground;

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = _theme.CodeBackground,
            BorderBrush = _theme.CodeBorder,
            BorderThickness = new Thickness(_theme.CodeBorderThickness),
            CornerRadius = _theme.CodeCornerRadius,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 6, 0, _theme.BlockSpacing),
            Content = block
        };
    }

    private Rectangle RenderHorizontalRule()
    {
        return new Rectangle
        {
            Height = 1,
            Fill = _theme.HrStroke,
            Opacity = 0.3,
            Margin = new Thickness(0, 12, 0, 12)
        };
    }

    private Grid RenderTable(Table table, int nestingLevel)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 8, 0, _theme.BlockSpacing)
        };

        var rowIndex = 0;
        var maxColumns = table.OfType<TableRow>().Select(row => row.Count).DefaultIfEmpty(0).Max();
        for (var column = 0; column < maxColumns; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        foreach (var row in table.OfType<TableRow>())
        {
            var isHeader = row.IsHeader;
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var columnIndex = 0;
            foreach (var cell in row.OfType<TableCell>())
            {
                var content = CreateStackPanel();
                foreach (var child in cell)
                {
                    content.Children.Add(RenderBlock(child, nestingLevel));
                }

                var border = new Border
                {
                    BorderBrush = _theme.TableBorder,
                    BorderThickness = new Thickness(1),
                    Background = isHeader ? _theme.TableHeaderBackground : null,
                    Padding = new Thickness(8, 6, 8, 6),
                    Child = content
                };
                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, columnIndex++);
                grid.Children.Add(border);
            }

            rowIndex++;
        }

        return grid;
    }

    private RichTextBlock RenderRawText(string text)
    {
        var block = CreateRichTextBlock();
        block.Blocks.Add(new Paragraph { Inlines = { new Run { Text = text } } });
        return block;
    }

    private RichTextBlock CreateRichTextBlock()
    {
        return new RichTextBlock
        {
            FontFamily = _theme.BodyFont,
            FontSize = _theme.BodyFontSize,
            Foreground = _theme.Foreground,
            TextWrapping = TextWrapping.WrapWholeWords,
            Margin = new Thickness(0, 2, 0, _theme.ParagraphSpacing)
        };
    }

    private StackPanel CreateStackPanel()
    {
        return new StackPanel
        {
            Spacing = 2,
            Padding = new Thickness(0)
        };
    }

    private void AppendInlines(InlineCollection target, ContainerInline? container, int skipLeadingCharacters = 0)
    {
        var inline = container?.FirstChild;
        var remainingSkip = skipLeadingCharacters;
        while (inline is not null)
        {
            AppendInline(target, inline, ref remainingSkip);
            inline = inline.NextSibling;
        }
    }

    private void AppendInline(InlineCollection target, MarkdigInline inline, ref int skipLeadingCharacters)
    {
        switch (inline)
        {
            case LiteralInline literal:
                var literalText = literal.Content.ToString();
                if (skipLeadingCharacters > 0)
                {
                    var skip = Math.Min(skipLeadingCharacters, literalText.Length);
                    literalText = literalText[skip..];
                    skipLeadingCharacters -= skip;
                }

                if (literalText.Length > 0)
                {
                    target.Add(new Run { Text = literalText });
                }
                break;
            case LineBreakInline:
                target.Add(new LineBreak());
                break;
            case CodeInline code:
                target.Add(CreateInlineCode(code.Content));
                break;
            case EmphasisInline emphasis:
                AppendEmphasis(target, emphasis);
                break;
            case LinkInline { IsImage: true } image:
                target.Add(CreateInlineImage(image));
                break;
            case LinkInline link:
                target.Add(CreateLink(link));
                break;
            case HtmlInline html:
                target.Add(new Run { Text = html.Tag });
                break;
            case ContainerInline container:
                AppendInlines(target, container, skipLeadingCharacters);
                skipLeadingCharacters = 0;
                break;
            default:
                target.Add(new Run { Text = inline.ToString() ?? string.Empty });
                break;
        }
    }

    private void AppendEmphasis(InlineCollection target, EmphasisInline emphasis)
    {
        var span = emphasis.DelimiterChar == '*' || emphasis.DelimiterChar == '_'
            ? emphasis.DelimiterCount >= 2 ? new Bold() : new Italic()
            : new Span();

        if (emphasis.DelimiterChar == '~')
        {
            span.TextDecorations = TextDecorations.Strikethrough;
        }

        AppendInlines(span.Inlines, emphasis);
        target.Add(span);
    }

    private InlineUIContainer CreateInlineCode(string text)
    {
        return new InlineUIContainer
        {
            Child = new Border
            {
                Background = _theme.CodeBackground,
                BorderBrush = _theme.CodeBorder,
                BorderThickness = new Thickness(_theme.CodeBorderThickness),
                CornerRadius = _theme.InlineCodeCornerRadius,
                Padding = new Thickness(4, 0, 4, 1),
                Child = new TextBlock
                {
                    Text = text,
                    FontFamily = _theme.CodeFont,
                    FontSize = 13,
                    Foreground = _theme.CodeForeground
                }
            }
        };
    }

    private Hyperlink CreateLink(LinkInline link)
    {
        var hyperlink = new Hyperlink { Foreground = _theme.LinkForeground };
        AppendInlines(hyperlink.Inlines, link);
        hyperlink.Click += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(link.Url))
            {
                LinkClicked?.Invoke(this, new MarkdownLinkEventArgs(link.Url, link.Title));
            }
        };
        return hyperlink;
    }

    private InlineUIContainer CreateInlineImage(LinkInline image)
    {
        var bitmap = new BitmapImage();
        if (Uri.TryCreate(image.Url, UriKind.RelativeOrAbsolute, out var uri))
        {
            bitmap.UriSource = uri;
        }

        return new InlineUIContainer
        {
            Child = new Image
            {
                Source = bitmap,
                MaxWidth = _theme.MaxImageWidth,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 4, 0, 4)
            }
        };
    }

    private bool? TryGetTaskState(ListItemBlock item)
    {
        var firstParagraph = item.OfType<ParagraphBlock>().FirstOrDefault();
        var text = InlineText(firstParagraph?.Inline).TrimStart();
        if (text.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (text.StartsWith("[ ] ", StringComparison.Ordinal))
        {
            return false;
        }

        return null;
    }

    private FontWeight HeadingWeight(int index)
    {
        return _theme.HeadingFontWeights.Length > index ? _theme.HeadingFontWeights[index] : FontWeights.SemiBold;
    }

    private string InlineText(ContainerInline? inline)
    {
        if (inline is null)
        {
            return string.Empty;
        }

        var text = new StringWriter();
        var child = inline.FirstChild;
        while (child is not null)
        {
            text.Write(child switch
            {
                LiteralInline literal => literal.Content.ToString(),
                CodeInline code => code.Content,
                LineBreakInline => Environment.NewLine,
                LinkInline link => InlineText(link),
                ContainerInline container => InlineText(container),
                _ => child.ToString()
            });
            child = child.NextSibling;
        }

        return text.ToString();
    }
}
