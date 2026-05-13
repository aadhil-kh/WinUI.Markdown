using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
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
        var root = CreateRichTextBlock();
        root.IsTextSelectionEnabled = true;

        var blockIndex = 0;
        foreach (var block in document)
        {
            AddRenderedBlock(root.Blocks, RenderBlock(block, 0, blockIndex == 0));
            blockIndex++;
        }

        return new Border
        {
            Background = _theme.Background,
            Padding = new Thickness(0),
            Child = root
        };
    }

    private void AddRenderedBlock(BlockCollection target, FrameworkElement element)
    {
        if (element is RichTextBlock richTextBlock)
        {
            while (richTextBlock.Blocks.Count > 0)
            {
                var block = richTextBlock.Blocks[0];
                richTextBlock.Blocks.RemoveAt(0);
                target.Add(block);
            }

            return;
        }

        RenderUiBlock(target, element);
    }

    private static void RenderUiBlock(BlockCollection target, UIElement child)
    {
        target.Add(new Paragraph
        {
            Margin = new Thickness(0),
            Inlines =
            {
                new InlineUIContainer
                {
                    Child = child
                }
            }
        });
    }

    private FrameworkElement RenderBlock(MarkdigBlock block, int nestingLevel, bool isFirstInContainer = false)
    {
        return block switch
        {
            HeadingBlock heading => RenderHeading(heading, isFirstInContainer),
            ParagraphBlock paragraph => RenderParagraph(paragraph),
            QuoteBlock quote => RenderQuote(quote, nestingLevel),
            ListBlock list => RenderList(list, nestingLevel),
            FencedCodeBlock code => RenderCodeBlock(code),
            CodeBlock code => RenderCodeBlock(code),
            ThematicBreakBlock => RenderHorizontalRule(),
            Table table => RenderTable(table, nestingLevel),
            FootnoteGroup footnotes => RenderFootnoteGroup(footnotes),
            DefinitionList definitionList => RenderDefinitionList(definitionList, nestingLevel),
            LinkReferenceDefinitionGroup => RenderNothing(),
            HtmlBlock html => RenderRawText(html.Lines.ToString()),
            _ => RenderRawText(block.ToString() ?? string.Empty)
        };
    }

    private FrameworkElement RenderHeading(HeadingBlock heading, bool isFirstInContainer)
    {
        var sizeIndex = Math.Clamp(heading.Level - 1, 0, 5);
        var fontSize = HeadingSize(sizeIndex);
        var borderThickness = HeadingBorderThickness(sizeIndex);
        var block = CreateRichTextBlock();
        block.Margin = new Thickness(0, isFirstInContainer ? 0 : _theme.HeadingSpacingBefore, 0, _theme.HeadingSpacingAfter);
        block.FontSize = fontSize;
        block.FontWeight = HeadingWeight(sizeIndex);
        block.LineHeight = fontSize * 1.25;

        var paragraph = new Paragraph();
        AppendInlines(paragraph.Inlines, heading.Inline);
        block.Blocks.Add(paragraph);

        if (borderThickness <= 0)
        {
            return block;
        }

        return new Border
        {
            BorderBrush = _theme.HeadingBorder,
            BorderThickness = new Thickness(0, 0, 0, borderThickness),
            Padding = new Thickness(0, 0, 0, fontSize * _theme.HeadingBorderPaddingEm),
            Margin = new Thickness(0, isFirstInContainer ? 0 : _theme.HeadingSpacingBefore, 0, _theme.HeadingSpacingAfter),
            Child = block
        };
    }

    private RichTextBlock RenderParagraph(
        ParagraphBlock paragraph,
        int skipLeadingCharacters = 0,
        bool skipLeadingTaskList = false,
        double? paragraphSpacing = null)
    {
        var block = CreateRichTextBlock();
        block.Margin = new Thickness(0, 0, 0, paragraphSpacing ?? _theme.ParagraphSpacing);
        var xamlParagraph = new Microsoft.UI.Xaml.Documents.Paragraph();
        AppendInlines(xamlParagraph.Inlines, paragraph.Inline, skipLeadingCharacters, skipLeadingTaskList);
        block.Blocks.Add(xamlParagraph);
        return block;
    }

    private Border RenderQuote(QuoteBlock quote, int nestingLevel)
    {
        var panel = CreateStackPanel();
        var childIndex = 0;
        foreach (var child in quote)
        {
            panel.Children.Add(RenderBlock(child, nestingLevel + 1, childIndex == 0));
            childIndex++;
        }

        return new Border
        {
            BorderBrush = _theme.BlockquoteAccent,
            Background = _theme.BlockquoteBackground,
            BorderThickness = new Thickness(4, 0, 0, 0),
            CornerRadius = _theme.BlockquoteCornerRadius,
            Padding = new Thickness(12, 4, 10, 4),
            Margin = new Thickness(0, 0, 0, _theme.BlockSpacing),
            Child = panel
        };
    }

    private MarkdownListPanel RenderList(ListBlock list, int nestingLevel)
    {
        var panel = new MarkdownListPanel
        {
            IsOrdered = list.IsOrdered,
            StartNumber = int.TryParse(list.OrderedStart, out var start) ? start : 1,
            OrderedDelimiter = list.OrderedDelimiter,
            NestingLevel = nestingLevel,
            Theme = _theme,
            Margin = new Thickness(0, 0, 0, _theme.ListSpacing)
        };

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var itemPanel = CreateStackPanel();
            var taskState = TryGetTaskState(item);
            var skippedTaskPrefix = false;
            var itemChildCount = item.Count;
            foreach (var child in item)
            {
                if (!skippedTaskPrefix && taskState.HasValue && child is ParagraphBlock paragraph)
                {
                    itemPanel.Children.Add(RenderParagraph(paragraph, skipLeadingTaskList: true, paragraphSpacing: ListItemParagraphSpacing(list, itemChildCount)));
                    skippedTaskPrefix = true;
                }
                else if (child is ParagraphBlock childParagraph)
                {
                    itemPanel.Children.Add(RenderParagraph(childParagraph, paragraphSpacing: ListItemParagraphSpacing(list, itemChildCount)));
                }
                else
                {
                    itemPanel.Children.Add(RenderBlock(child, nestingLevel + 1, itemPanel.Children.Count == 0));
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
        var paragraph = new Microsoft.UI.Xaml.Documents.Paragraph();
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
            Margin = new Thickness(0, 0, 0, _theme.BlockSpacing),
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
            Margin = new Thickness(0, _theme.BlockSpacing, 0, _theme.BlockSpacing)
        };
    }

    private FrameworkElement RenderTable(Table table, int nestingLevel)
    {
        var grid = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Left
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
                    content.Children.Add(child is ParagraphBlock paragraph
                        ? RenderParagraph(paragraph, paragraphSpacing: 0)
                        : RenderBlock(child, nestingLevel));
                }

                var border = new Border
                {
                    BorderBrush = _theme.TableBorder,
                    BorderThickness = new Thickness(1),
                    Background = TableCellBackground(rowIndex, isHeader),
                    Padding = new Thickness(13, 6, 13, 6),
                    Child = content
                };
                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, columnIndex++);
                grid.Children.Add(border);
            }

            rowIndex++;
        }

        return new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, _theme.BlockSpacing),
            Content = grid
        };
    }

    private FrameworkElement RenderFootnoteGroup(FootnoteGroup footnotes)
    {
        var panel = CreateStackPanel();
        foreach (var footnote in footnotes.OfType<Footnote>().OrderBy(footnote => footnote.Order))
        {
            var row = new Grid
            {
                ColumnSpacing = 8,
                Margin = new Thickness(0, 0, 0, 4)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            row.Children.Add(new TextBlock
            {
                Text = $"{FootnoteDisplayIndex(footnote)}.",
                FontFamily = _theme.BodyFont,
                FontSize = _theme.BodyFontSize * 0.85,
                LineHeight = _theme.BodyFontSize * 1.5,
                Foreground = _theme.Foreground,
                HorizontalAlignment = HorizontalAlignment.Right
            });

            var content = CreateStackPanel();
            foreach (var child in footnote)
            {
                content.Children.Add(RenderBlock(child, 0, content.Children.Count == 0));
            }

            Grid.SetColumn(content, 1);
            row.Children.Add(content);
            panel.Children.Add(row);
        }

        return new Border
        {
            BorderBrush = _theme.HrStroke,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Margin = new Thickness(0, _theme.BlockSpacing, 0, 0),
            Padding = new Thickness(0, _theme.BlockSpacing / 2, 0, 0),
            Child = panel
        };
    }

    private FrameworkElement RenderDefinitionList(DefinitionList definitionList, int nestingLevel)
    {
        var panel = CreateStackPanel();
        foreach (var item in definitionList.OfType<DefinitionItem>())
        {
            foreach (var child in item)
            {
                if (child is DefinitionTerm term)
                {
                    panel.Children.Add(RenderDefinitionTerm(term));
                }
                else
                {
                    panel.Children.Add(RenderDefinitionContent(child, nestingLevel));
                }
            }
        }

        panel.Margin = new Thickness(0, 0, 0, _theme.BlockSpacing);
        return panel;
    }

    private FrameworkElement RenderDefinitionTerm(DefinitionTerm term)
    {
        var block = CreateRichTextBlock();
        block.Margin = new Thickness(0);
        block.FontWeight = FontWeights.SemiBold;
        var paragraph = new Microsoft.UI.Xaml.Documents.Paragraph();
        AppendInlines(paragraph.Inlines, term.Inline);
        block.Blocks.Add(paragraph);
        return block;
    }

    private FrameworkElement RenderDefinitionContent(MarkdigBlock block, int nestingLevel)
    {
        var content = block is ParagraphBlock paragraph
            ? RenderParagraph(paragraph, paragraphSpacing: _theme.ParagraphSpacing / 2)
            : RenderBlock(block, nestingLevel + 1);

        content.Margin = MergeLeftMargin(content.Margin, 24);
        return content;
    }

    private static FrameworkElement RenderNothing()
    {
        return new Border
        {
            Visibility = Visibility.Collapsed,
            Height = 0,
            Width = 0
        };
    }

    private Brush? TableCellBackground(int rowIndex, bool isHeader)
    {
        if (isHeader)
        {
            return _theme.TableHeaderBackground;
        }

        return rowIndex % 2 == 1 ? _theme.TableRowAlternateBackground : null;
    }

    private double ListItemParagraphSpacing(ListBlock list, int itemChildCount)
    {
        return list.IsLoose || itemChildCount > 1 ? _theme.ParagraphSpacing : 0;
    }

    private RichTextBlock RenderRawText(string text)
    {
        var block = CreateRichTextBlock();
        block.Blocks.Add(new Microsoft.UI.Xaml.Documents.Paragraph { Inlines = { new Run { Text = text } } });
        return block;
    }

    private RichTextBlock CreateRichTextBlock()
    {
        return new RichTextBlock
        {
            FontFamily = _theme.BodyFont,
            FontSize = _theme.BodyFontSize,
            Foreground = _theme.Foreground,
            LineHeight = _theme.BodyFontSize * 1.5,
            TextWrapping = TextWrapping.WrapWholeWords,
            Margin = new Thickness(0, 0, 0, _theme.ParagraphSpacing)
        };
    }

    private StackPanel CreateStackPanel()
    {
        return new StackPanel
        {
            Spacing = 0,
            Padding = new Thickness(0)
        };
    }

    private void AppendInlines(
        InlineCollection target,
        ContainerInline? container,
        int skipLeadingCharacters = 0,
        bool skipLeadingTaskList = false)
    {
        var inline = container?.FirstChild;
        var remainingSkip = skipLeadingCharacters;
        var skipTaskList = skipLeadingTaskList;
        while (inline is not null)
        {
            AppendInline(target, inline, ref remainingSkip, ref skipTaskList);
            inline = inline.NextSibling;
        }
    }

    private void AppendInline(
        InlineCollection target,
        MarkdigInline inline,
        ref int skipLeadingCharacters,
        ref bool skipLeadingTaskList)
    {
        switch (inline)
        {
            case TaskList taskList:
                if (skipLeadingTaskList)
                {
                    skipLeadingTaskList = false;
                    break;
                }

                target.Add(CreateInlineTaskCheckbox(taskList.Checked));
                break;
            case FootnoteLink footnoteLink:
                if (!footnoteLink.IsBackLink)
                {
                    target.Add(CreateFootnoteReference(footnoteLink));
                }
                break;
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
                AppendInlines(target, container, skipLeadingCharacters, skipLeadingTaskList);
                skipLeadingCharacters = 0;
                skipLeadingTaskList = false;
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
                Padding = new Thickness(3, 0, 3, 0),
                Margin = new Thickness(0, 2, 0, -2),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = text,
                    FontFamily = _theme.CodeFont,
                    FontSize = _theme.BodyFontSize * 0.85,
                    LineHeight = _theme.BodyFontSize * 1.05,
                    Foreground = _theme.CodeForeground
                }
            }
        };
    }

    private InlineUIContainer CreateInlineTaskCheckbox(bool isChecked)
    {
        return new InlineUIContainer
        {
            Child = new CheckBox
            {
                IsChecked = isChecked,
                IsHitTestVisible = false,
                IsTabStop = false,
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(0, 0, 4, -3)
            }
        };
    }

    private InlineUIContainer CreateFootnoteReference(FootnoteLink footnoteLink)
    {
        return new InlineUIContainer
        {
            Child = new TextBlock
            {
                Text = $"[{FootnoteDisplayIndex(footnoteLink)}]",
                FontFamily = _theme.BodyFont,
                FontSize = _theme.BodyFontSize * 0.72,
                Foreground = _theme.LinkForeground,
                Margin = new Thickness(1, 0, 1, _theme.BodyFontSize * 0.45)
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
                MaxWidth = _theme.MaxImageWidth > 0 ? _theme.MaxImageWidth : double.PositiveInfinity,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 4, 0, 4)
            }
        };
    }

    private bool? TryGetTaskState(ListItemBlock item)
    {
        var firstParagraph = item.OfType<ParagraphBlock>().FirstOrDefault();
        if (firstParagraph?.Inline?.FirstChild is TaskList taskList)
        {
            return taskList.Checked;
        }

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

    private double HeadingSize(int index)
    {
        if (_theme.HeadingSizes.Length == 0)
        {
            return _theme.BodyFontSize;
        }

        return _theme.HeadingSizes.Length > index ? _theme.HeadingSizes[index] : _theme.HeadingSizes[^1];
    }

    private double HeadingBorderThickness(int index)
    {
        return _theme.HeadingBorderThicknesses.Length > index ? _theme.HeadingBorderThicknesses[index] : 0;
    }

    private static int FootnoteDisplayIndex(Footnote footnote)
    {
        return footnote.Order >= 0 ? footnote.Order + 1 : 1;
    }

    private static int FootnoteDisplayIndex(FootnoteLink footnoteLink)
    {
        return footnoteLink.Footnote is not null ? FootnoteDisplayIndex(footnoteLink.Footnote) : 1;
    }

    private static Thickness MergeLeftMargin(Thickness margin, double left)
    {
        return new Thickness(margin.Left + left, margin.Top, margin.Right, margin.Bottom);
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
