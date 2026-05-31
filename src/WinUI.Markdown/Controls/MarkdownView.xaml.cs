using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Markdig;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Text.Json;
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

    public static readonly DependencyProperty ViewModeProperty =
        DependencyProperty.Register(nameof(ViewMode), typeof(MarkdownViewMode), typeof(MarkdownView), new PropertyMetadata(MarkdownViewMode.PreviewOnly, OnEditorConfigurationChanged));

    public static readonly DependencyProperty MonacoAssetsPathProperty =
        DependencyProperty.Register(nameof(MonacoAssetsPath), typeof(string), typeof(MarkdownView), new PropertyMetadata(string.Empty, OnEditorConfigurationChanged));

    public static readonly DependencyProperty MonacoExtensionScriptPathProperty =
        DependencyProperty.Register(nameof(MonacoExtensionScriptPath), typeof(string), typeof(MarkdownView), new PropertyMetadata(string.Empty, OnEditorConfigurationChanged));

    public static readonly DependencyProperty EditorLanguageProperty =
        DependencyProperty.Register(nameof(EditorLanguage), typeof(string), typeof(MarkdownView), new PropertyMetadata("markdown", OnMonacoAppearanceChanged));

    public static readonly DependencyProperty MonacoThemeProperty =
        DependencyProperty.Register(nameof(MonacoTheme), typeof(MonacoEditorTheme), typeof(MarkdownView), new PropertyMetadata(MonacoEditorTheme.System, OnMonacoAppearanceChanged));

    private IMarkdownRenderer? _renderer;
    private readonly EventHandler<MarkdownLinkEventArgs> _linkClickedHandler;
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();
    private RenderMode? _activeRenderMode;
    private WebView2? _monacoEditor;
    private bool _monacoReady;
    private string? _monacoMappedFolder;
    private bool _isUpdatingFromEditor;
    private bool _isUpdatingEditorText;
    private bool _isSplitterDragging;
    private bool _isDualPaneLayoutApplied;
    private double _splitterDragStartX;
    private double _splitterEditorStartWidth;
    private double? _savedEditorWidth;
    private const string MonacoHost = "markdown-editor.local";

    public MarkdownView()
    {
        _linkClickedHandler = (_, args) => LinkClicked?.Invoke(this, args);
        InitializeComponent();
        InitializeSplitter();
        ActualThemeChanged += OnActualThemeChanged;
        Loaded += async (_, _) =>
        {
            ApplyEditorLayout();
            await RenderAsync();
        };
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

    public MarkdownViewMode ViewMode
    {
        get => (MarkdownViewMode)GetValue(ViewModeProperty);
        set => SetValue(ViewModeProperty, value);
    }

    public string MonacoAssetsPath
    {
        get => (string)GetValue(MonacoAssetsPathProperty);
        set => SetValue(MonacoAssetsPathProperty, value);
    }

    public string EditorLanguage
    {
        get => (string)GetValue(EditorLanguageProperty);
        set => SetValue(EditorLanguageProperty, value);
    }

    public string MonacoExtensionScriptPath
    {
        get => (string)GetValue(MonacoExtensionScriptPathProperty);
        set => SetValue(MonacoExtensionScriptPathProperty, value);
    }

    public MonacoEditorTheme MonacoTheme
    {
        get => (MonacoEditorTheme)GetValue(MonacoThemeProperty);
        set => SetValue(MonacoThemeProperty, value);
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

    private static async void OnEditorConfigurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownView view)
        {
            view.ResetEditorHost();
            view.ApplyEditorLayout();
            await view.RenderAsync();
        }
    }

    private static async void OnMonacoAppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MarkdownView view)
        {
            await view.ApplyMonacoAppearanceAsync();
        }
    }

    private async void OnActualThemeChanged(FrameworkElement sender, object args)
    {
        if (MonacoTheme == MonacoEditorTheme.System)
        {
            await ApplyMonacoAppearanceAsync();
        }

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

        ApplyEditorLayout();

        if (ViewMode == MarkdownViewMode.DualPane)
        {
            var editorReady = await EnsureEditorHostAsync();
            if (!editorReady)
            {
                ApplyPreviewOnlyLayout();
            }
        }
        else
        {
            ResetEditorHost();
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

        if (!_isUpdatingFromEditor)
        {
            await SyncEditorTextAsync(markdown);
        }
    }

    private void ApplyEditorLayout()
    {
        if (ViewMode == MarkdownViewMode.DualPane)
        {
            if (_isDualPaneLayoutApplied)
            {
                return;
            }

            PART_EditorHost.Visibility = Visibility.Visible;
            PART_Splitter.Visibility = Visibility.Visible;
            PART_SplitterColumn.Width = new GridLength(8);

            if (_savedEditorWidth.HasValue && _savedEditorWidth.Value > 0)
            {
                PART_EditorColumn.Width = new GridLength(_savedEditorWidth.Value);
                PART_PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                PART_EditorColumn.Width = new GridLength(1, GridUnitType.Star);
                PART_PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
            }

            _isDualPaneLayoutApplied = true;
            return;
        }

        ApplyPreviewOnlyLayout();
    }

    private async Task<bool> EnsureEditorHostAsync()
    {
        if (!TryResolveMonacoAssetsRoot(out var assetsRoot))
        {
            ResetEditorHost();
            return false;
        }

        EnsureMonacoHost();
        await EnsureMonacoReadyAsync(assetsRoot);
        return true;
    }

    private void EnsureMonacoHost()
    {
        if (_monacoEditor is not null)
        {
            return;
        }

        var editor = new WebView2
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _monacoEditor = editor;
        PART_EditorHost.Child = editor;
    }

    private async Task EnsureMonacoReadyAsync(string assetsRoot)
    {
        if (_monacoEditor is null)
        {
            return;
        }

        var extensionScriptUrl = ResolveMonacoExtensionScriptUrl(assetsRoot);

        await _monacoEditor.EnsureCoreWebView2Async();
        _monacoEditor.CoreWebView2.WebMessageReceived -= OnMonacoWebMessageReceived;
        _monacoEditor.CoreWebView2.WebMessageReceived += OnMonacoWebMessageReceived;

        var navigated = false;

        if (!string.Equals(_monacoMappedFolder, assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            _monacoEditor.CoreWebView2.SetVirtualHostNameToFolderMapping(
                MonacoHost,
                assetsRoot,
                CoreWebView2HostResourceAccessKind.Allow);
            _monacoMappedFolder = assetsRoot;
            _monacoReady = false;
            _monacoEditor.NavigateToString(BuildMonacoHtml(EditorLanguage, extensionScriptUrl, ResolveMonacoThemeName()));
            navigated = true;
        }

        if (!_monacoReady && !navigated)
        {
            _monacoEditor.NavigateToString(BuildMonacoHtml(EditorLanguage, extensionScriptUrl, ResolveMonacoThemeName()));
        }
    }

    private bool TryResolveMonacoAssetsRoot(out string assetsRoot)
    {
        assetsRoot = string.Empty;

        if (string.IsNullOrWhiteSpace(MonacoAssetsPath))
        {
            return false;
        }

        var configuredPath = MonacoAssetsPath.Trim();
        var fullPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));

        var loaderPath = Path.Combine(fullPath, "vs", "loader.js");
        if (!File.Exists(loaderPath))
        {
            return false;
        }

        assetsRoot = fullPath;
        return true;
    }

    private string? ResolveMonacoExtensionScriptUrl(string assetsRoot)
    {
        if (string.IsNullOrWhiteSpace(MonacoExtensionScriptPath))
        {
            return null;
        }

        var normalizedRelativePath = MonacoExtensionScriptPath
            .Trim()
            .Replace('\\', '/')
            .TrimStart('/');

        var fullPath = Path.GetFullPath(Path.Combine(
            assetsRoot,
            normalizedRelativePath.Replace('/', Path.DirectorySeparatorChar)));

        if (!fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!File.Exists(fullPath))
        {
            return null;
        }

        return $"https://{MonacoHost}/{normalizedRelativePath}";
    }

    private static string BuildMonacoHtml(string language, string? extensionScriptUrl, string initialTheme)
    {
        var editorLanguage = string.IsNullOrWhiteSpace(language) ? "markdown" : language;
        var extensionScriptTag = string.IsNullOrWhiteSpace(extensionScriptUrl)
            ? string.Empty
            : $"<script src=\"{extensionScriptUrl}\"></script>";

        return $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <style>
                html, body, #container { margin: 0; width: 100%; height: 100%; overflow: hidden; background: transparent; }
              </style>
              <script src="https://{{MonacoHost}}/vs/loader.js"></script>
                            {{extensionScriptTag}}
            </head>
            <body>
              <div id="container"></div>
              <script>
                let editor = null;
                let isApplyingExternalUpdate = false;
                require.config({ paths: { vs: 'https://{{MonacoHost}}/vs' } });
                require(['vs/editor/editor.main'], function () {
                  editor = monaco.editor.create(document.getElementById('container'), {
                    value: '',
                    language: '{{editorLanguage}}',
                    theme: '{{initialTheme}}',
                    automaticLayout: true,
                    minimap: { enabled: false },
                    wordWrap: 'on',
                    scrollBeyondLastLine: false
                  });

                  window.setEditorText = function (value) {
                    if (!editor) {
                      return;
                    }

                    if (editor.getValue() === (value || '')) {
                      return;
                    }

                    isApplyingExternalUpdate = true;
                    editor.setValue(value || '');
                    isApplyingExternalUpdate = false;
                  };

                                    window.setEditorTheme = function (themeName) {
                                        if (!editor) {
                                            return;
                                        }

                                        monaco.editor.setTheme(themeName || 'vs');
                                    };

                                    window.setEditorLanguage = function (languageName) {
                                        if (!editor) {
                                            return;
                                        }

                                        const model = editor.getModel();
                                        if (model) {
                                            monaco.editor.setModelLanguage(model, languageName || 'markdown');
                                        }
                                    };

                                    if (typeof window.configureMarkdownViewMonaco === 'function') {
                                        try {
                                            window.configureMarkdownViewMonaco({ monaco: monaco, editor: editor, language: '{{editorLanguage}}' });
                                        } catch (error) {
                                            console.error('Monaco extension script failed', error);
                                        }
                                    }

                  editor.onDidChangeModelContent(function () {
                    if (isApplyingExternalUpdate) {
                      return;
                    }

                    window.chrome.webview.postMessage(JSON.stringify({ type: 'change', value: editor.getValue() }));
                  });

                  window.chrome.webview.postMessage(JSON.stringify({ type: 'ready' }));
                });
              </script>
            </body>
            </html>
            """;
    }

    private void OnMonacoWebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        var payload = args.TryGetWebMessageAsString();
        if (string.IsNullOrWhiteSpace(payload))
        {
            return;
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        if (!root.TryGetProperty("type", out var typeElement))
        {
            return;
        }

        var type = typeElement.GetString();
        if (string.Equals(type, "ready", StringComparison.OrdinalIgnoreCase))
        {
            _monacoReady = true;
            _ = ApplyMonacoAppearanceAsync();
            _ = SyncMonacoTextAsync(Text ?? string.Empty);
            return;
        }

        if (!string.Equals(type, "change", StringComparison.OrdinalIgnoreCase) || _isUpdatingEditorText)
        {
            return;
        }

        var value = root.TryGetProperty("value", out var valueElement)
            ? valueElement.GetString() ?? string.Empty
            : string.Empty;
        UpdateTextFromEditor(value);
    }

    private void UpdateTextFromEditor(string newText)
    {
        if (string.Equals(Text, newText, StringComparison.Ordinal))
        {
            return;
        }

        _isUpdatingFromEditor = true;
        try
        {
            SetValue(TextProperty, newText);
        }
        finally
        {
            _isUpdatingFromEditor = false;
        }
    }

    private async Task SyncEditorTextAsync(string markdown)
    {
        if (ViewMode != MarkdownViewMode.DualPane)
        {
            return;
        }

        _isUpdatingEditorText = true;
        try
        {
            if (_monacoEditor is not null)
            {
                await SyncMonacoTextAsync(markdown);
            }
        }
        finally
        {
            _isUpdatingEditorText = false;
        }
    }

    private async Task SyncMonacoTextAsync(string markdown)
    {
        if (_monacoEditor?.CoreWebView2 is null || !_monacoReady)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(markdown ?? string.Empty);
        await _monacoEditor.ExecuteScriptAsync($"window.setEditorText({payload});");
    }

    private async Task ApplyMonacoAppearanceAsync()
    {
        if (_monacoEditor?.CoreWebView2 is null || !_monacoReady)
        {
            return;
        }

        var themePayload = JsonSerializer.Serialize(ResolveMonacoThemeName());
        var languagePayload = JsonSerializer.Serialize(string.IsNullOrWhiteSpace(EditorLanguage) ? "markdown" : EditorLanguage);
        await _monacoEditor.ExecuteScriptAsync($"window.setEditorTheme({themePayload});");
        await _monacoEditor.ExecuteScriptAsync($"window.setEditorLanguage({languagePayload});");
    }

    private string ResolveMonacoThemeName()
    {
        return MonacoTheme switch
        {
            MonacoEditorTheme.Light => "vs",
            MonacoEditorTheme.Dark => "vs-dark",
            _ => ActualTheme == ElementTheme.Dark ? "vs-dark" : "vs"
        };
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

    private void ResetEditorHost()
    {
        if (_monacoEditor?.CoreWebView2 is not null)
        {
            _monacoEditor.CoreWebView2.WebMessageReceived -= OnMonacoWebMessageReceived;
        }

        _monacoEditor?.Close();

        _monacoEditor = null;
        _monacoReady = false;
        _monacoMappedFolder = null;
        PART_EditorHost.Child = null;
    }

    private void InitializeSplitter()
    {
        PART_Splitter.PointerEntered += (_, _) =>
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);

        PART_Splitter.PointerExited += (_, _) =>
        {
            if (!_isSplitterDragging)
                ProtectedCursor = null;
        };

        PART_Splitter.PointerPressed += OnSplitterPointerPressed;
        PART_Splitter.PointerMoved += OnSplitterPointerMoved;
        PART_Splitter.PointerReleased += OnSplitterPointerReleased;

        PART_Splitter.PointerCaptureLost += (_, _) =>
        {
            _isSplitterDragging = false;
            ProtectedCursor = null;
        };
    }

    private void OnSplitterPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (!e.GetCurrentPoint(PART_Splitter).Properties.IsLeftButtonPressed)
            return;

        _isSplitterDragging = true;
        _splitterDragStartX = e.GetCurrentPoint(PART_LayoutRoot).Position.X;
        _splitterEditorStartWidth = PART_EditorColumn.ActualWidth;
        ((UIElement)sender).CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnSplitterPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging)
            return;

        var currentX = e.GetCurrentPoint(PART_LayoutRoot).Position.X;
        var delta = currentX - _splitterDragStartX;
        var totalAvailableWidth = PART_LayoutRoot.ActualWidth - PART_Splitter.ActualWidth;
        var newEditorWidth = Math.Clamp(_splitterEditorStartWidth + delta, 100, totalAvailableWidth - 100);
        PART_EditorColumn.Width = new GridLength(newEditorWidth);
        _savedEditorWidth = newEditorWidth;
        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSplitterDragging)
            return;

        _isSplitterDragging = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        ProtectedCursor = null;
        e.Handled = true;
    }

    private void ApplyPreviewOnlyLayout()
    {
        if (_isDualPaneLayoutApplied && PART_EditorColumn.ActualWidth > 0)
        {
            _savedEditorWidth = PART_EditorColumn.ActualWidth;
        }

        PART_EditorHost.Visibility = Visibility.Collapsed;
        PART_Splitter.Visibility = Visibility.Collapsed;
        PART_EditorColumn.Width = new GridLength(0);
        PART_SplitterColumn.Width = new GridLength(0);
        PART_PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
        _isDualPaneLayoutApplied = false;
    }
}
