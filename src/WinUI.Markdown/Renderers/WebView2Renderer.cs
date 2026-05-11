using System.Net;
using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using WinUI.Markdown.Controls;
using WinUI.Markdown.Themes;

namespace WinUI.Markdown.Renderers;

internal sealed class WebView2Renderer : IMarkdownRenderer
{
    private readonly WebView2 _webView = new()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch
    };

    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    private bool _initialized;

    public event EventHandler<MarkdownLinkEventArgs>? LinkClicked;

    public FrameworkElement Element => _webView;

    public async Task RenderAsync(string markdown, MarkdownTheme theme)
    {
        await EnsureInitializedAsync();
        var html = Markdig.Markdown.ToHtml(markdown ?? string.Empty, _pipeline);
        _webView.NavigateToString(BuildHtmlShell(html, theme));
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _webView.EnsureCoreWebView2Async();
        _webView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            var url = args.TryGetWebMessageAsString();
            if (!string.IsNullOrWhiteSpace(url))
            {
                LinkClicked?.Invoke(this, new MarkdownLinkEventArgs(url));
            }
        };
        _initialized = true;
    }

    private static string BuildHtmlShell(string body, MarkdownTheme theme)
    {
        return $$"""
            <!doctype html>
            <html>
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1.0">
              <style>{{theme.ToCss()}}</style>
            </head>
            <body>
              <main id="content">{{body}}</main>
              <script>
                document.addEventListener('click', event => {
                  const anchor = event.target.closest('a[href]');
                  if (!anchor) return;
                  event.preventDefault();
                  window.chrome.webview.postMessage(anchor.href);
                });
              </script>
            </body>
            </html>
            """;
    }
}
