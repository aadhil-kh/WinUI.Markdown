# WinUI.Markdown

`WinUI.Markdown` is a WinUI 3 Markdown control for Windows App SDK apps. It supports native WinUI rendering for common Markdown, WebView2 rendering for rich HTML scenarios, and an Auto mode that chooses the right renderer for the input.

[![CI](https://github.com/aadhil-kh/WinUI.Markdown/actions/workflows/ci.yml/badge.svg)](https://github.com/aadhil-kh/WinUI.Markdown/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/WinUI.Markdown.svg)](https://www.nuget.org/packages/WinUI.Markdown)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Features

- `RenderMode.Auto`: uses native WinUI rendering when sufficient and falls back to WebView2 for unsupported Markdown/HTML.
- `RenderMode.Native`: creates a WinUI element tree from a Markdig AST.
- `RenderMode.WebView2`: renders Markdig HTML inside a lazy WebView2 host.
- `MarkdownViewMode.PreviewOnly`: preview-only mode (default).
- `MarkdownViewMode.DualPane`: editor + preview mode in a single control.
- Monaco editor provider for dual-pane mode via app-supplied assets path.
- Monaco editor theme options: `System` (default), `Light`, and `Dark`.
- Shared themes for native and WebView2 rendering.
- Built-in WinUI, GitHub, and Dracula themes.
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
    ViewMode="PreviewOnly"
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
Viewer.ViewMode = MarkdownViewMode.DualPane;
Viewer.MonacoAssetsPath = "Assets\\monaco";
Viewer.EditorLanguage = "markdown";
Viewer.MonacoTheme = MonacoEditorTheme.System;
Viewer.Theme = MarkdownTheme.GitHubLight;
```

Dual-pane mode uses Monaco:

```csharp
Viewer.ViewMode = MarkdownViewMode.DualPane;
Viewer.MonacoAssetsPath = "Assets\\monaco"; // must contain vs/loader.js
Viewer.MonacoExtensionScriptPath = "extensions/markdown.sample.js"; // optional, resolved under MonacoAssetsPath
Viewer.EditorLanguage = "markdown";
Viewer.MonacoTheme = MonacoEditorTheme.Dark;
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
- `ViewMode`: preview-only or dual-pane editor+preview.
- `MonacoAssetsPath`: required for dual-pane mode; points to a folder with `vs/loader.js`.
- `MonacoExtensionScriptPath`: optional extension script path (relative to `MonacoAssetsPath`) for custom Monaco language features.
- `EditorLanguage`: syntax language value passed to Monaco.
- `MonacoTheme`: editor theme selection (`System`, `Light`, `Dark`).

`ViewMode` defaults to `MarkdownViewMode.PreviewOnly`, so existing preview-only usage does not change.

## Native Rendering

Native mode currently supports the common Markdown and GFM surface without creating WebView2. WebView2 mode renders Markdig HTML and is the fallback for raw HTML and richer extension output.

| Markdown element | Native renderer | WebView2 renderer |
|---|---|---|
| Paragraphs and line breaks | Supported | Supported |
| ATX/setext headings | Supported | Supported |
| Bold, italic, strikethrough | Supported | Supported |
| Inline code | Supported | Supported |
| Code blocks and fenced code | Supported, with lightweight highlighting | Supported |
| Links and reference links | Supported | Supported |
| Images | Supported, with configurable max width | Supported, with configurable max width |
| Ordered lists | Supported, including nested roman/alpha markers | Supported |
| Unordered lists | Supported, including nested bullet shapes | Supported |
| Task lists | Supported as read-only WinUI checkboxes | Supported as read-only HTML checkboxes |
| Blockquotes | Supported | Supported |
| Tables | Supported | Supported |
| Horizontal rules | Supported | Supported |
| Footnotes | Supported | Supported |
| Definition lists | Supported | Supported |
| Raw HTML blocks/inlines | Not supported; Auto falls back | Supported |
| Math extensions | Not supported; Auto falls back | Supported when emitted by Markdig |
| Figures and diagrams | Not supported; Auto falls back | Supported when emitted by Markdig |

## Themes

Built-in themes:

- `MarkdownTheme.System`
- `MarkdownTheme.WinUILight`
- `MarkdownTheme.WinUIDark`
- `MarkdownTheme.GitHubLight`
- `MarkdownTheme.GitHubDark`
- `MarkdownTheme.Dracula`

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

To refresh Monaco assets in the sample app:

```powershell
cd samples\WinUI.Markdown.Sample
npm install
npm run sync-monaco
```

The sample app also includes `Assets/monaco/extensions/markdown.sample.js`, wired through `MonacoExtensionScriptPath`, which adds markdown snippets and editor configuration in Monaco.

Create a package:

```powershell
dotnet pack src\WinUI.Markdown\MarkdownView.csproj -c Release --no-restore -o artifacts
```

## Publishing

Publishing is handled by `.github/workflows/publish.yml` on `v*` tags. Add a NuGet.org API key as the `NUGET_API_KEY` repository secret before publishing.

See [docs/RELEASE.md](docs/RELEASE.md) for the release checklist.

## License

MIT. See [LICENSE](LICENSE).
