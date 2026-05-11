using Microsoft.UI.Xaml;
using WinUI.Markdown.Controls;
using WinUI.Markdown.Themes;

namespace WinUI.Markdown.Renderers;

internal interface IMarkdownRenderer
{
    event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    FrameworkElement Element { get; }

    Task RenderAsync(string markdown, MarkdownTheme theme);
}
