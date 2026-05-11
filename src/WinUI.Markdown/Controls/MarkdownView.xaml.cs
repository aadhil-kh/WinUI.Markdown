using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Markdig;
using WinUI.Markdown.Renderers;
using WinUI.Markdown.Themes;

namespace WinUI.Markdown.Controls;

public sealed partial class MarkdownView : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MarkdownView), new PropertyMetadata(string.Empty, OnRenderPropertyChanged));

    public static readonly DependencyProperty RenderModeProperty =
        DependencyProperty.Register(nameof(RenderMode), typeof(RenderMode), typeof(MarkdownView), new PropertyMetadata(RenderMode.Auto, OnRenderModeChanged));

    public static readonly DependencyProperty ThemeProperty =
        DependencyProperty.Register(nameof(Theme), typeof(MarkdownTheme), typeof(MarkdownView), new PropertyMetadata(MarkdownTheme.System, OnRenderPropertyChanged));

    public static readonly DependencyProperty ActualRenderModeProperty =
        DependencyProperty.Register(nameof(ActualRenderMode), typeof(RenderMode), typeof(MarkdownView), new PropertyMetadata(RenderMode.Native));

    public static readonly DependencyProperty AllowWebView2FallbackProperty =
        DependencyProperty.Register(nameof(AllowWebView2Fallback), typeof(bool), typeof(MarkdownView), new PropertyMetadata(true, OnRenderModeChanged));

    public static readonly DependencyProperty AutoFallbackReasonProperty =
        DependencyProperty.Register(nameof(AutoFallbackReason), typeof(string), typeof(MarkdownView), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MaxImageWidthProperty =
        DependencyProperty.Register(nameof(MaxImageWidth), typeof(double), typeof(MarkdownView), new PropertyMetadata(double.NaN, OnRenderPropertyChanged));

    private IMarkdownRenderer? _renderer;
    private readonly EventHandler<MarkdownLinkEventArgs> _linkClickedHandler;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();
    private RenderMode? _activeRenderMode;

    public MarkdownView()
    {
        _linkClickedHandler = (_, args) => LinkClicked?.Invoke(this, args);
        InitializeComponent();
        ActualThemeChanged += OnActualThemeChanged;
        Loaded += async (_, _) => await RenderAsync();
    }

    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    public event EventHandler<MarkdownRenderedEventArgs>? Rendered;

    public event EventHandler<MarkdownRenderedEventArgs>? ActualRenderModeChanged;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public RenderMode RenderMode
    {
        get => (RenderMode)GetValue(RenderModeProperty);
        set => SetValue(RenderModeProperty, value);
    }

    public MarkdownTheme Theme
    {
        get => (MarkdownTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    public RenderMode ActualRenderMode
    {
        get => (RenderMode)GetValue(ActualRenderModeProperty);
        private set => SetValue(ActualRenderModeProperty, value);
    }

    public bool AllowWebView2Fallback
    {
        get => (bool)GetValue(AllowWebView2FallbackProperty);
        set => SetValue(AllowWebView2FallbackProperty, value);
    }

    public string AutoFallbackReason
    {
        get => (string)GetValue(AutoFallbackReasonProperty);
        private set => SetValue(AutoFallbackReasonProperty, value);
    }

    public double MaxImageWidth
    {
        get => (double)GetValue(MaxImageWidthProperty);
        set => SetValue(MaxImageWidthProperty, value);
    }

    private static async void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownView view)
        {
            await view.RenderAsync();
        }
    }

    private static async void OnRenderModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownView view)
        {
            view.ResetRenderer();
            await view.RenderAsync();
        }
    }

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (Theme.TracksSystemTheme)
        {
            await RenderAsync();
        }
    }

    private async Task RenderAsync()
    {
        if (!IsLoaded)
        {
            return;
        }

        var markdown = Text ?? string.Empty;
        var document = Markdig.Markdown.Parse(markdown, _pipeline);
        var renderPlan = MarkdownRenderPlanner.Resolve(RenderMode, document, AllowWebView2Fallback);
        var previousRenderMode = ActualRenderMode;
        var effectiveRenderMode = renderPlan.ActualRenderMode;
        ActualRenderMode = effectiveRenderMode;
        AutoFallbackReason = renderPlan.FallbackReason;

        var renderer = EnsureRenderer(effectiveRenderMode);
        var resolvedTheme = Theme.ResolveFor(this);
        if (!double.IsNaN(MaxImageWidth) && MaxImageWidth > 0)
        {
            resolvedTheme.MaxImageWidth = MaxImageWidth;
        }

        await renderer.RenderAsync(Text ?? string.Empty, resolvedTheme);
        var args = new MarkdownRenderedEventArgs(RenderMode, effectiveRenderMode);
        if (previousRenderMode != effectiveRenderMode)
        {
            ActualRenderModeChanged?.Invoke(this, args);
        }

        Rendered?.Invoke(this, args);
    }

    private IMarkdownRenderer EnsureRenderer(RenderMode effectiveRenderMode)
    {
        if (_renderer is not null && _activeRenderMode == effectiveRenderMode)
        {
            return _renderer;
        }

        ResetRenderer();
        _activeRenderMode = effectiveRenderMode;
        _renderer = effectiveRenderMode == RenderMode.WebView2
            ? new WebView2Renderer()
            : new NativeRenderer();
        _renderer.LinkClicked += _linkClickedHandler;

        PART_Root.Children.Clear();
        PART_Root.Children.Add(_renderer.Element);
        return _renderer;
    }

    private void ResetRenderer()
    {
        if (_renderer is not null)
        {
            _renderer.LinkClicked -= _linkClickedHandler;
        }

        _renderer = null;
        _activeRenderMode = null;
        PART_Root.Children.Clear();
    }
}
