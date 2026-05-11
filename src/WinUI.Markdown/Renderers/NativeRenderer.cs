using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI.Markdown.Controls;
using WinUI.Markdown.Themes;
using WinUI.Markdown.Visitors;

namespace WinUI.Markdown.Renderers;

internal sealed class NativeRenderer : IMarkdownRenderer
{
    private readonly ScrollViewer _scrollViewer = new()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    public FrameworkElement Element => _scrollViewer;

    public Task RenderAsync(string markdown, MarkdownTheme theme)
    {
        var document = Markdig.Markdown.Parse(markdown ?? string.Empty, _pipeline);
        var visitor = new NativeMarkdownVisitor(theme);
        visitor.LinkClicked += (_, args) => LinkClicked?.Invoke(this, args);
        _scrollViewer.Background = theme.Background;
        _scrollViewer.Content = visitor.Render(document);
        return Task.CompletedTask;
    }
}
