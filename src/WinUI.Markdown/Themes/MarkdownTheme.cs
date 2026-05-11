using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace WinUI.Markdown.Themes;

public class MarkdownTheme
{
    public FontFamily BodyFont { get; set; } = new("Segoe UI");

    public FontFamily CodeFont { get; set; } = new("Cascadia Mono, Consolas");

    public double BodyFontSize { get; set; } = 14;

    public double[] HeadingSizes { get; set; } = [32, 28, 24, 20, 18, 16];

    public Windows.UI.Text.FontWeight[] HeadingFontWeights { get; set; } =
    [
        FontWeights.SemiBold,
        FontWeights.SemiBold,
        FontWeights.SemiBold,
        FontWeights.SemiBold,
        FontWeights.SemiBold,
        FontWeights.SemiBold
    ];

    public double HeadingSpacingBefore { get; set; } = 16;

    public double HeadingSpacingAfter { get; set; } = 6;

    public double ParagraphSpacing { get; set; } = 8;

    public double BlockSpacing { get; set; } = 10;

    public double ListSpacing { get; set; } = 8;

    public Brush Background { get; set; } = Solid("#FFFFFFFF");

    public Brush Foreground { get; set; } = Solid("#FF111111");

    public Brush CodeBackground { get; set; } = Solid("#14000000");

    public Brush CodeForeground { get; set; } = Solid("#FF24292F");

    public Brush CodeBorder { get; set; } = Solid("#00000000");

    public double CodeBorderThickness { get; set; } = 0;

    public CornerRadius CodeCornerRadius { get; set; } = new(6);

    public CornerRadius InlineCodeCornerRadius { get; set; } = new(4);

    public Brush BlockquoteAccent { get; set; } = Solid("#FF60789C");

    public Brush BlockquoteBackground { get; set; } = Solid("#08000000");

    public CornerRadius BlockquoteCornerRadius { get; set; } = new(0);

    public Brush LinkForeground { get; set; } = Solid("#FF005FB8");

    public Brush TableBorder { get; set; } = Solid("#50000000");

    public Brush TableHeaderBackground { get; set; } = Solid("#0A000000");

    public Brush HrStroke { get; set; } = Solid("#78000000");

    public double MaxImageWidth { get; set; } = 720;

    public string CustomCss { get; set; } = string.Empty;

    public bool TracksSystemTheme { get; init; }

    public static MarkdownTheme WinUILight => new();

    public static MarkdownTheme WinUIDark => new()
    {
        Background = Solid("#FF1F1F1F"),
        Foreground = Solid("#FFF3F3F3"),
        CodeBackground = Solid("#26FFFFFF"),
        CodeForeground = Solid("#FFF3F3F3"),
        CodeBorder = Solid("#22FFFFFF"),
        CodeBorderThickness = 1,
        BlockquoteAccent = Solid("#FF8EA6CF"),
        BlockquoteBackground = Solid("#10FFFFFF"),
        LinkForeground = Solid("#FF70B2FF"),
        TableBorder = Solid("#5AFFFFFF"),
        TableHeaderBackground = Solid("#12FFFFFF"),
        HrStroke = Solid("#82FFFFFF")
    };

    public static MarkdownTheme GitHubLight => new()
    {
        BodyFont = new FontFamily("-apple-system, BlinkMacSystemFont, Segoe UI"),
        CodeFont = new FontFamily("ui-monospace, SFMono-Regular, SF Mono, Consolas"),
        Background = Solid("#FFFFFFFF"),
        Foreground = Solid("#FF1F2328"),
        CodeBackground = Solid("#0D818B98"),
        CodeForeground = Solid("#FF1F2328"),
        CodeBorder = Solid("#FFD0D7DE"),
        CodeBorderThickness = 0,
        BlockquoteAccent = Solid("#FFD0D7DE"),
        BlockquoteBackground = Solid("#00FFFFFF"),
        LinkForeground = Solid("#FF0969DA"),
        TableBorder = Solid("#FFD0D7DE"),
        TableHeaderBackground = Solid("#FFF6F8FA"),
        HrStroke = Solid("#FFD8DEE4")
    };

    public static MarkdownTheme GitHubDark => new()
    {
        BodyFont = new FontFamily("-apple-system, BlinkMacSystemFont, Segoe UI"),
        CodeFont = new FontFamily("ui-monospace, SFMono-Regular, SF Mono, Consolas"),
        Background = Solid("#FF0D1117"),
        Foreground = Solid("#FFE6EDF3"),
        CodeBackground = Solid("#33A6A6A6"),
        CodeForeground = Solid("#FFE6EDF3"),
        CodeBorder = Solid("#FF30363D"),
        CodeBorderThickness = 0,
        BlockquoteAccent = Solid("#FF30363D"),
        BlockquoteBackground = Solid("#00FFFFFF"),
        LinkForeground = Solid("#FF2F81F7"),
        TableBorder = Solid("#FF30363D"),
        TableHeaderBackground = Solid("#FF161B22"),
        HrStroke = Solid("#FF21262D")
    };

    public static MarkdownTheme Light => WinUILight;

    public static MarkdownTheme Dark => WinUIDark;

    public static MarkdownTheme System => new() { TracksSystemTheme = true };

    internal MarkdownTheme ResolveFor(FrameworkElement element)
    {
        if (!TracksSystemTheme)
        {
            return this;
        }

        var palette = element.ActualTheme == ElementTheme.Dark ? WinUIDark : WinUILight;
        return WithPalette(palette);
    }

    private MarkdownTheme WithPalette(MarkdownTheme palette)
    {
        return new MarkdownTheme
        {
            BodyFont = BodyFont,
            CodeFont = CodeFont,
            BodyFontSize = BodyFontSize,
            HeadingSizes = [.. HeadingSizes],
            HeadingFontWeights = [.. HeadingFontWeights],
            HeadingSpacingBefore = HeadingSpacingBefore,
            HeadingSpacingAfter = HeadingSpacingAfter,
            ParagraphSpacing = ParagraphSpacing,
            BlockSpacing = BlockSpacing,
            ListSpacing = ListSpacing,
            Background = palette.Background,
            Foreground = palette.Foreground,
            CodeBackground = palette.CodeBackground,
            CodeForeground = palette.CodeForeground,
            CodeBorder = palette.CodeBorder,
            CodeBorderThickness = palette.CodeBorderThickness,
            CodeCornerRadius = CodeCornerRadius,
            InlineCodeCornerRadius = InlineCodeCornerRadius,
            BlockquoteAccent = palette.BlockquoteAccent,
            BlockquoteBackground = palette.BlockquoteBackground,
            BlockquoteCornerRadius = BlockquoteCornerRadius,
            LinkForeground = palette.LinkForeground,
            TableBorder = palette.TableBorder,
            TableHeaderBackground = palette.TableHeaderBackground,
            HrStroke = palette.HrStroke,
            MaxImageWidth = MaxImageWidth,
            CustomCss = CustomCss
        };
    }

    internal string ToCss()
    {
        return $$"""
            :root {
              color-scheme: light dark;
              --md-bg: {{CssColor(Background)}};
              --md-fg: {{CssColor(Foreground)}};
              --md-font-body: {{CssFontFamily(BodyFont, "Segoe UI, sans-serif")}};
              --md-font-code: {{CssFontFamily(CodeFont, "Cascadia Mono, Consolas, monospace")}};
              --md-font-size: {{BodyFontSize.ToString(global::System.Globalization.CultureInfo.InvariantCulture)}}px;
              --md-link: {{CssColor(LinkForeground)}};
              --md-code-bg: {{CssColor(CodeBackground)}};
              --md-code-fg: {{CssColor(CodeForeground)}};
              --md-border: {{CssColor(TableBorder)}};
              --md-table-head-bg: {{CssColor(TableHeaderBackground)}};
              --md-quote: {{CssColor(BlockquoteAccent)}};
              --md-quote-bg: {{CssColor(BlockquoteBackground)}};
              --md-rule: {{CssColor(HrStroke)}};
              --md-h1: {{HeadingSize(0)}}px;
              --md-h2: {{HeadingSize(1)}}px;
              --md-h3: {{HeadingSize(2)}}px;
              --md-h4: {{HeadingSize(3)}}px;
              --md-h5: {{HeadingSize(4)}}px;
              --md-h6: {{HeadingSize(5)}}px;
              --md-h1-weight: {{HeadingWeight(0)}};
              --md-h2-weight: {{HeadingWeight(1)}};
              --md-h3-weight: {{HeadingWeight(2)}};
              --md-h4-weight: {{HeadingWeight(3)}};
              --md-h5-weight: {{HeadingWeight(4)}};
              --md-h6-weight: {{HeadingWeight(5)}};
              --md-heading-before: {{CssPx(HeadingSpacingBefore)}};
              --md-heading-after: {{CssPx(HeadingSpacingAfter)}};
              --md-paragraph-spacing: {{CssPx(ParagraphSpacing)}};
              --md-block-spacing: {{CssPx(BlockSpacing)}};
              --md-list-spacing: {{CssPx(ListSpacing)}};
              --md-code-border: {{CssColor(CodeBorder)}};
              --md-code-border-width: {{CssPx(CodeBorderThickness)}};
              --md-code-radius: {{CssRadius(CodeCornerRadius)}};
              --md-inline-code-radius: {{CssRadius(InlineCodeCornerRadius)}};
              --md-blockquote-radius: {{CssRadius(BlockquoteCornerRadius)}};
            }
            html, body { margin: 0; padding: 0; background: var(--md-bg); color: var(--md-fg); font: var(--md-font-size)/1.55 var(--md-font-body); }
            body { overflow-wrap: anywhere; }
            h1 { font-size: var(--md-h1); }
            h2 { font-size: var(--md-h2); }
            h3 { font-size: var(--md-h3); }
            h4 { font-size: var(--md-h4); }
            h5 { font-size: var(--md-h5); }
            h6 { font-size: var(--md-h6); }
            h1 { font-weight: var(--md-h1-weight); }
            h2 { font-weight: var(--md-h2-weight); }
            h3 { font-weight: var(--md-h3-weight); }
            h4 { font-weight: var(--md-h4-weight); }
            h5 { font-weight: var(--md-h5-weight); }
            h6 { font-weight: var(--md-h6-weight); }
            h1, h2, h3, h4, h5, h6 { line-height: 1.25; margin: var(--md-heading-before) 0 var(--md-heading-after); }
            h1:first-child, h2:first-child, h3:first-child { margin-top: 0; }
            p { margin: 0 0 var(--md-paragraph-spacing); }
            ul, ol { margin-top: 0; margin-bottom: var(--md-list-spacing); }
            a { color: var(--md-link); }
            pre, code { font-family: var(--md-font-code); }
            code { background: var(--md-code-bg); border-radius: var(--md-inline-code-radius); padding: 0.1em 0.35em; }
            pre { background: var(--md-code-bg); color: var(--md-code-fg); border: var(--md-code-border-width) solid var(--md-code-border); border-radius: var(--md-code-radius); overflow: auto; padding: 12px; margin: 0 0 var(--md-block-spacing); }
            pre code { background: transparent; padding: 0; }
            blockquote { border-left: 4px solid var(--md-quote); background: var(--md-quote-bg); border-radius: var(--md-blockquote-radius); margin: 0 0 var(--md-block-spacing); padding: 4px 0 4px 12px; color: var(--md-fg); opacity: 0.78; }
            table { border-collapse: collapse; width: 100%; margin: 0 0 var(--md-block-spacing); }
            th, td { border: 1px solid var(--md-border); padding: 6px 8px; }
            th { background: var(--md-table-head-bg); font-weight: 600; }
            hr { border: 0; border-top: 1px solid var(--md-rule); opacity: 0.7; }
            img { max-width: 100%; height: auto; }
            {{CustomCss}}
            """;
    }

    private double HeadingSize(int index)
    {
        return HeadingSizes.Length > index ? HeadingSizes[index] : HeadingSizes.LastOrDefault(BodyFontSize);
    }

    private ushort HeadingWeight(int index)
    {
        return HeadingFontWeights.Length > index ? HeadingFontWeights[index].Weight : FontWeights.SemiBold.Weight;
    }

    private static string CssPx(double value)
    {
        return $"{value.ToString(global::System.Globalization.CultureInfo.InvariantCulture)}px";
    }

    private static string CssRadius(CornerRadius radius)
    {
        return $"{CssPx(radius.TopLeft)} {CssPx(radius.TopRight)} {CssPx(radius.BottomRight)} {CssPx(radius.BottomLeft)}";
    }

    internal static string CssColor(Brush brush)
    {
        if (brush is not SolidColorBrush solid)
        {
            return "transparent";
        }

        var color = solid.Color;
        var alpha = Math.Round(color.A / 255d, 3).ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return $"rgba({color.R}, {color.G}, {color.B}, {alpha})";
    }

    private static string CssFontFamily(FontFamily fontFamily, string fallback)
    {
        var source = fontFamily.Source;
        return string.IsNullOrWhiteSpace(source) ? fallback : source;
    }

    private static SolidColorBrush Solid(string value)
    {
        var hex = value.TrimStart('#');
        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }

        var parsed = uint.Parse(hex, global::System.Globalization.NumberStyles.HexNumber);
        var color = Color.FromArgb(
            (byte)(parsed >> 24),
            (byte)(parsed >> 16),
            (byte)(parsed >> 8),
            (byte)parsed);
        return new SolidColorBrush(color);
    }
}
