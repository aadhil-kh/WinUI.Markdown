using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Windows.UI;
using WinUI.Markdown.Controls;
using WinUI.Markdown.Themes;

namespace MarkdownViewTest;

public sealed partial class MainWindow : Window
{
    private bool _isInitialized;
    private bool _isApplyingPreset;
    private readonly DispatcherQueueTimer _themeDebounceTimer;

    public MainWindow()
    {
        InitializeComponent();
        _themeDebounceTimer = DispatcherQueue.CreateTimer();
        _themeDebounceTimer.Interval = TimeSpan.FromMilliseconds(120);
        _themeDebounceTimer.Tick += (_, _) =>
        {
            _themeDebounceTimer.Stop();
            ApplyTheme();
        };
        MarkdownEditor.Text = SampleMarkdown;
        Viewer.Text = SampleMarkdown;
        ApplyControlOptions();
        ApplyThemePresetFields();
        ApplyTheme();
        ApplyViewVisibility();
        _isInitialized = true;
    }

    private string SampleMarkdown { get; } = """
        # WinUI.Markdown

        Type on the left and tune the control on the right.

        ## Native-friendly content

        - **Bold**, *italic*, ~~strike~~, and `inline code`
        - [x] Task lists render as read-only native checkboxes
        - [ ] Nested lists, tables, images, and horizontal rules
        - [Link click events](https://github.com/your-org/WinUI.Markdown)

        > Auto mode keeps this in native WinUI until the input needs the HTML renderer.

        | Feature | Native |
        | --- | --- |
        | Headings | Yes |
        | Tables | Yes |
        | Raw HTML | WebView2 fallback |

        ```csharp
        public sealed class Demo
        {
            public string Name { get; init; } = "native";
        }
        ```

        ---

        <details><summary>Raw HTML makes Auto choose WebView2</summary>HTML block</details>
        """;

    private void OnMarkdownChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        Viewer.Text = MarkdownEditor.Text;
    }

    private void OnRenderModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        Viewer.RenderMode = RenderModeBox.SelectedIndex switch
        {
            1 => RenderMode.Native,
            2 => RenderMode.WebView2,
            _ => RenderMode.Auto
        };
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitialized)
        {
            ApplyThemePresetFields();
            ApplyTheme();
        }
    }

    private void OnThemeValueChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitialized && !_isApplyingPreset)
        {
            QueueApplyTheme();
        }
    }

    private void OnThemeSliderChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_isInitialized && !_isApplyingPreset)
        {
            QueueApplyTheme();
        }
    }

    private void QueueApplyTheme()
    {
        _themeDebounceTimer.Stop();
        _themeDebounceTimer.Start();
    }

    private void ApplyTheme()
    {
        var theme = ThemeBox.SelectedIndex switch
        {
            1 => MarkdownTheme.WinUILight,
            2 => MarkdownTheme.WinUIDark,
            3 => MarkdownTheme.GitHubLight,
            4 => MarkdownTheme.GitHubDark,
            5 => MarkdownTheme.Dracula,
            _ => MarkdownTheme.System
        };

        theme.BodyFont = new FontFamily(BodyFontBox.Text);
        theme.CodeFont = new FontFamily(CodeFontBox.Text);
        theme.BodyFontSize = BodySize.Value;
        theme.HeadingSizes = [H1Size.Value, H2Size.Value, H3Size.Value, H4Size.Value, H5Size.Value, H6Size.Value];
        theme.HeadingFontWeights =
        [
            FontWeightFromSlider(H1Weight.Value),
            FontWeightFromSlider(H2Weight.Value),
            FontWeightFromSlider(H3Weight.Value),
            FontWeightFromSlider(H4Weight.Value),
            FontWeightFromSlider(H5Weight.Value),
            FontWeightFromSlider(H6Weight.Value)
        ];
        theme.HeadingSpacingBefore = HeadingBefore.Value;
        theme.HeadingSpacingAfter = HeadingAfter.Value;
        theme.HeadingBorder = BrushFromText(HeadingBorderBox.Text, theme.HeadingBorder);
        theme.HeadingBorderThicknesses = [H1BorderThickness.Value, H2BorderThickness.Value, 0, 0, 0, 0];
        theme.HeadingBorderPaddingEm = HeadingBorderPadding.Value;
        theme.ParagraphSpacing = ParagraphSpacing.Value;
        theme.BlockSpacing = BlockSpacing.Value;
        theme.ListSpacing = ListSpacing.Value;
        theme.Background = BrushFromText(BackgroundBox.Text, theme.Background);
        theme.Foreground = BrushFromText(ForegroundBox.Text, theme.Foreground);
        theme.CodeBackground = BrushFromText(CodeBackgroundBox.Text, theme.CodeBackground);
        theme.CodeForeground = BrushFromText(CodeForegroundBox.Text, theme.CodeForeground);
        theme.CodeBorder = BrushFromText(CodeBorderBox.Text, theme.CodeBorder);
        theme.CodeBorderThickness = CodeBorderThickness.Value;
        theme.CodeCornerRadius = new CornerRadius(CodeCornerRadius.Value);
        theme.InlineCodeCornerRadius = new CornerRadius(InlineCodeCornerRadius.Value);
        theme.LinkForeground = BrushFromText(LinkForegroundBox.Text, theme.LinkForeground);
        theme.BlockquoteAccent = BrushFromText(BlockquoteAccentBox.Text, theme.BlockquoteAccent);
        theme.BlockquoteBackground = BrushFromText(BlockquoteBackgroundBox.Text, theme.BlockquoteBackground);
        theme.BlockquoteCornerRadius = new CornerRadius(BlockquoteCornerRadius.Value);
        theme.TableBorder = BrushFromText(TableBorderBox.Text, theme.TableBorder);
        theme.TableHeaderBackground = BrushFromText(TableHeaderBackgroundBox.Text, theme.TableHeaderBackground);
        theme.TableRowAlternateBackground = BrushFromText(TableRowAlternateBackgroundBox.Text, theme.TableRowAlternateBackground);
        theme.HrStroke = BrushFromText(HrStrokeBox.Text, theme.HrStroke);
        theme.MaxImageWidth = MaxImageWidth.Value;
        theme.CustomCss = CustomCssBox.Text;

        Viewer.Theme = theme;
        Viewer.MaxImageWidth = MaxImageWidth.Value;
        PreviewHost.Background = theme.TracksSystemTheme ? ResolveSystemPreviewBackground() : theme.Background;
    }

    private void ApplyThemePresetFields()
    {
        _isApplyingPreset = true;
        var preset = ThemeBox.SelectedIndex switch
        {
            1 => MarkdownTheme.WinUILight,
            2 => MarkdownTheme.WinUIDark,
            3 => MarkdownTheme.GitHubLight,
            4 => MarkdownTheme.GitHubDark,
            5 => MarkdownTheme.Dracula,
            _ => Viewer.ActualTheme == ElementTheme.Dark ? MarkdownTheme.WinUIDark : MarkdownTheme.WinUILight
        };

        BodyFontBox.Text = preset.BodyFont.Source;
        CodeFontBox.Text = preset.CodeFont.Source;
        BodySize.Value = preset.BodyFontSize;
        H1Size.Value = HeadingSize(preset, 0);
        H2Size.Value = HeadingSize(preset, 1);
        H3Size.Value = HeadingSize(preset, 2);
        H4Size.Value = HeadingSize(preset, 3);
        H5Size.Value = HeadingSize(preset, 4);
        H6Size.Value = HeadingSize(preset, 5);
        H1Weight.Value = HeadingWeight(preset, 0);
        H2Weight.Value = HeadingWeight(preset, 1);
        H3Weight.Value = HeadingWeight(preset, 2);
        H4Weight.Value = HeadingWeight(preset, 3);
        H5Weight.Value = HeadingWeight(preset, 4);
        H6Weight.Value = HeadingWeight(preset, 5);
        HeadingBefore.Value = preset.HeadingSpacingBefore;
        HeadingAfter.Value = preset.HeadingSpacingAfter;
        HeadingBorderBox.Text = HexFromBrush(preset.HeadingBorder);
        H1BorderThickness.Value = HeadingBorderThickness(preset, 0);
        H2BorderThickness.Value = HeadingBorderThickness(preset, 1);
        HeadingBorderPadding.Value = preset.HeadingBorderPaddingEm;
        ParagraphSpacing.Value = preset.ParagraphSpacing;
        BlockSpacing.Value = preset.BlockSpacing;
        ListSpacing.Value = preset.ListSpacing;
        BackgroundBox.Text = HexFromBrush(preset.Background);
        ForegroundBox.Text = HexFromBrush(preset.Foreground);
        CodeBackgroundBox.Text = HexFromBrush(preset.CodeBackground);
        CodeForegroundBox.Text = HexFromBrush(preset.CodeForeground);
        CodeBorderBox.Text = HexFromBrush(preset.CodeBorder);
        LinkForegroundBox.Text = HexFromBrush(preset.LinkForeground);
        BlockquoteAccentBox.Text = HexFromBrush(preset.BlockquoteAccent);
        BlockquoteBackgroundBox.Text = HexFromBrush(preset.BlockquoteBackground);
        TableBorderBox.Text = HexFromBrush(preset.TableBorder);
        TableHeaderBackgroundBox.Text = HexFromBrush(preset.TableHeaderBackground);
        TableRowAlternateBackgroundBox.Text = HexFromBrush(preset.TableRowAlternateBackground);
        HrStrokeBox.Text = HexFromBrush(preset.HrStroke);
        CodeBorderThickness.Value = preset.CodeBorderThickness;
        CodeCornerRadius.Value = preset.CodeCornerRadius.TopLeft;
        InlineCodeCornerRadius.Value = preset.InlineCodeCornerRadius.TopLeft;
        BlockquoteCornerRadius.Value = preset.BlockquoteCornerRadius.TopLeft;
        MaxImageWidth.Value = preset.MaxImageWidth;
        _isApplyingPreset = false;
    }

    private static double HeadingSize(MarkdownTheme theme, int index)
    {
        return theme.HeadingSizes.Length > index ? theme.HeadingSizes[index] : theme.BodyFontSize;
    }

    private static double HeadingWeight(MarkdownTheme theme, int index)
    {
        return theme.HeadingFontWeights.Length > index ? theme.HeadingFontWeights[index].Weight : 600;
    }

    private static double HeadingBorderThickness(MarkdownTheme theme, int index)
    {
        return theme.HeadingBorderThicknesses.Length > index ? theme.HeadingBorderThicknesses[index] : 0;
    }

    private SolidColorBrush ResolveSystemPreviewBackground()
    {
        var theme = Viewer.ActualTheme == ElementTheme.Dark ? MarkdownTheme.WinUIDark : MarkdownTheme.WinUILight;
        return theme.Background as SolidColorBrush ?? new SolidColorBrush(Colors.Transparent);
    }

    private static Windows.UI.Text.FontWeight FontWeightFromSlider(double value)
    {
        return new Windows.UI.Text.FontWeight { Weight = (ushort)Math.Clamp(Math.Round(value / 100) * 100, 100, 900) };
    }

    private static SolidColorBrush BrushFromText(string text, Brush fallback)
    {
        return TryParseColor(text, out var color)
            ? new SolidColorBrush(color)
            : fallback as SolidColorBrush ?? new SolidColorBrush(Colors.Transparent);
    }

    private static bool TryParseColor(string value, out Color color)
    {
        color = Colors.Transparent;
        var hex = value.Trim().TrimStart('#');
        if (hex.Length == 6)
        {
            hex = "FF" + hex;
        }

        if (hex.Length != 8 || !uint.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var parsed))
        {
            return false;
        }

        color = Color.FromArgb(
            (byte)(parsed >> 24),
            (byte)(parsed >> 16),
            (byte)(parsed >> 8),
            (byte)parsed);
        return true;
    }

    private static string HexFromBrush(Brush brush)
    {
        if (brush is not SolidColorBrush solid)
        {
            return "#000000";
        }

        var color = solid.Color;
        return color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void OnViewToggleChanged(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            ApplyViewVisibility();
        }
    }

    private void ApplyViewVisibility()
    {
        var showExternalInput = ShowInputToggle.IsChecked == true && Viewer.ViewMode == MarkdownViewMode.PreviewOnly;
        SetColumnVisibility(MarkdownEditor, InputColumn, showExternalInput, 360);
        SetColumnVisibility(PreviewHost, PreviewColumn, ShowPreviewToggle.IsChecked == true, 1, true);
        SetColumnVisibility(PropsPanel, PropsColumn, ShowPropsToggle.IsChecked == true, 320);
    }

    private static void SetColumnVisibility(UIElement element, ColumnDefinition column, bool isVisible, double width, bool star = false)
    {
        element.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        column.Width = isVisible
            ? star ? new GridLength(width, GridUnitType.Star) : new GridLength(width)
            : new GridLength(0);
    }

    private void OnMarkdownRendered(object sender, MarkdownRenderedEventArgs e)
    {
        ActualModeText.Text = $"Requested: {e.RequestedRenderMode}   Actual: {e.ActualRenderMode}";
        FallbackReasonText.Text = string.IsNullOrWhiteSpace(Viewer.AutoFallbackReason)
            ? "Auto fallback: none"
            : $"Auto fallback: {Viewer.AutoFallbackReason}";
    }

    private void OnControlOptionChanged(object sender, RoutedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ApplyControlOptions();
    }

    private void OnControlOptionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ApplyControlOptions();
    }

    private void OnControlOptionTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized)
        {
            return;
        }

        ApplyControlOptions();
    }

    private void ApplyControlOptions()
    {
        Viewer.AllowWebView2Fallback = AllowFallbackToggle.IsChecked == true;
        Viewer.ViewMode = ViewModeBox.SelectedIndex == 1 ? MarkdownViewMode.DualPane : MarkdownViewMode.PreviewOnly;
        Viewer.MonacoAssetsPath = MonacoPathBox.Text;
        Viewer.MonacoExtensionScriptPath = "extensions/markdown.sample.js";
        Viewer.EditorLanguage = "markdown";
        Viewer.MonacoTheme = MonacoThemeBox.SelectedIndex switch
        {
            1 => MonacoEditorTheme.Light,
            2 => MonacoEditorTheme.Dark,
            _ => MonacoEditorTheme.System
        };
        ApplyViewVisibility();
    }

    private async void OnLinkClicked(object sender, MarkdownLinkEventArgs e)
    {
        e.Handled = true;
        var dialog = new ContentDialog
        {
            Title = "Link clicked",
            Content = e.Url,
            CloseButtonText = "OK",
            XamlRoot = Viewer.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
