# WinUI.Markdown

`WinUI.Markdown` is a WinUI 3 Markdown control for Windows App SDK apps. It supports native WinUI rendering for common Markdown, WebView2 rendering for rich HTML scenarios, and an Auto mode that chooses the right renderer for the input.

[![CI](https://github.com/aadhil-kh/WinUI.Markdown/actions/workflows/ci.yml/badge.svg)](https://github.com/aadhil-kh/WinUI.Markdown/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/WinUI.Markdown.svg)](https://www.nuget.org/packages/WinUI.Markdown)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- `RenderMode.Auto`: uses native WinUI rendering when sufficient and falls back to WebView2 for unsupported Markdown/HTML.
- `RenderMode.Native`: creates a WinUI element tree from a Markdig AST.
- `RenderMode.WebView2`: renders Markdig HTML inside a lazy WebView2 host.
- Shared themes for native and WebView2 rendering.
- Built-in WinUI and GitHub light/dark themes.
- Link click events routed through `MarkdownLinkEventArgs`.
- Sample playground app for testing render modes and theme properties.

## Install

```powershell
dotnet add package WinUI.Markdown
```

## Usage

Add the namespace:

```xml
xmlns:md="using:WinUI.Markdown.Controls"
```

Use the control:

```xml
<md:MarkdownView
    Text="{Binding MarkdownSource}"
    RenderMode="Auto"
    Theme="{x:Bind MarkdownTheme}"
    LinkClicked="OnLinkClicked"
    Rendered="OnMarkdownRendered" />
```

In code:

```csharp
using WinUI.Markdown.Controls;
using WinUI.Markdown.Themes;

Viewer.Text = "# Hello WinUI.Markdown";
Viewer.RenderMode = RenderMode.Auto;
Viewer.Theme = MarkdownTheme.GitHubLight;
```

Handle links:

```csharp
private void OnLinkClicked(object sender, MarkdownLinkEventArgs e)
{
    e.Handled = true;
    // Open with your app's navigation policy.
}
```

## Render Modes

| Mode | Behavior |
|---|---|
| `Auto` | Parses the Markdown and uses native rendering when supported, otherwise WebView2. |
| `Native` | Always renders to WinUI elements. WebView2 is not created. |
| `WebView2` | Always renders Markdig HTML inside WebView2. |

`MarkdownView.ActualRenderMode` reports the effective renderer. `MarkdownRenderedEventArgs` includes both requested and actual render modes.

Useful control options:

- `AllowWebView2Fallback`: lets Auto mode fall back to WebView2 when native rendering is not sufficient.
- `AutoFallbackReason`: describes why Auto mode selected or would select WebView2.
- `ActualRenderModeChanged`: raised when the effective renderer changes.
- `MaxImageWidth`: optional control-level override for themed image width.

## Native Rendering

Native mode currently supports:

- Paragraphs and headings
- Bold, italic, strikethrough, inline code, and links
- Ordered, unordered, nested, and task lists
- Blockquotes
- Code blocks with lightweight native highlighting
- Tables with header styling
- Images with configurable max width
- Horizontal rules

When content requires unsupported extensions such as raw HTML, math, footnotes, figures, diagrams, or definition lists, Auto mode falls back to WebView2.

## Themes

Built-in themes:

- `MarkdownTheme.System`
- `MarkdownTheme.WinUILight`
- `MarkdownTheme.WinUIDark`
- `MarkdownTheme.GitHubLight`
- `MarkdownTheme.GitHubDark`

`MarkdownTheme.Light` and `MarkdownTheme.Dark` are aliases for WinUI light/dark.

Native and WebView2 rendering share the same theme model, including background, foreground, links, code blocks, code borders, code corner radius, quotes, blockquote background, blockquote corner radius, tables, table header background, rules, fonts, heading sizes, heading font weights, spacing, and image sizing.

## Requirements

- Windows 10 19041 or later target
- Windows App SDK
- .NET 10

The package currently targets `net10.0-windows10.0.19041.0`.

## Development

```powershell
dotnet restore MarkdownView.slnx
dotnet build MarkdownView.slnx -c Debug --no-restore
dotnet test tests\WinUI.Markdown.Tests\WinUI.Markdown.Tests.csproj -c Debug --no-restore --no-build
```

Create a package:

```powershell
dotnet pack src\WinUI.Markdown\MarkdownView.csproj -c Release --no-restore -o artifacts
```

## Publishing

Publishing is handled by `.github/workflows/publish.yml` on `v*` tags. Add a NuGet.org API key as the `NUGET_API_KEY` repository secret before publishing.

See [docs/RELEASE.md](docs/RELEASE.md) for the release checklist.

## License

MIT. See [LICENSE](LICENSE).
