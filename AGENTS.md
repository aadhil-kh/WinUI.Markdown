# AGENTS.md

Guidance for coding agents working in this repository.

## Project

This repo builds `WinUI.Markdown`, a WinUI 3 Markdown control package.

- NuGet package ID: `WinUI.Markdown`
- Primary namespace: `WinUI.Markdown.Controls`
- Target framework: `net10.0-windows10.0.19041.0`
- Windows App SDK: 1.5+ intended, currently referenced through project packages
- License: MIT

## Layout

- `src/WinUI.Markdown/` contains the package.
- `src/WinUI.Markdown/Controls/` contains `MarkdownView` and control event args.
- `src/WinUI.Markdown/Renderers/` contains renderer selection and WebView2/native renderer hosts.
- `src/WinUI.Markdown/Visitors/` contains native Markdig AST to WinUI element rendering.
- `src/WinUI.Markdown/Themes/` contains the theme model and default resource dictionary.
- `samples/WinUI.Markdown.Sample/` is the WinUI sample playground.
- `tests/WinUI.Markdown.Tests/` contains package tests.
- `.github/workflows/` contains CI and publish workflows.

## Commands

Use these from the repository root:

```powershell
dotnet restore MarkdownView.slnx
dotnet build MarkdownView.slnx -c Debug --no-restore
dotnet test tests\WinUI.Markdown.Tests\WinUI.Markdown.Tests.csproj -c Debug --no-restore --no-build
dotnet pack src\WinUI.Markdown\MarkdownView.csproj -c Debug --no-build --no-restore -o artifacts
```

If packages are missing, run restore before build/test/pack. Avoid committing `bin/`, `obj/`, `.vs/`, `artifacts/`, or `*.user`.

## Architecture Notes

`MarkdownView` exposes three render modes:

- `RenderMode.Auto`: parse input and use native rendering when supported, otherwise WebView2.
- `RenderMode.Native`: always use the native WinUI renderer.
- `RenderMode.WebView2`: always use the WebView2 HTML renderer.

`MarkdownView.ActualRenderMode` reports the effective renderer after auto selection. `MarkdownRenderedEventArgs` includes both requested and actual render modes.

`MarkdownView` also exposes editor presentation options:

- `MarkdownViewMode.PreviewOnly`: preview-only mode (default).
- `MarkdownViewMode.DualPane`: editor + preview in one control.

Dual-pane mode uses a WebView2-hosted Monaco editor provider.

Useful public control options include `AllowWebView2Fallback`, `AutoFallbackReason`, `ActualRenderModeChanged`, and `MaxImageWidth`. Keep these in sync with the sample app when their behavior changes.

For dual-pane mode, `MonacoAssetsPath` is required and must point to app-provided assets that include `vs/loader.js`. Do not bundle Monaco assets into the library package.

`MonacoTheme` controls Monaco appearance with `System` (default), `Light`, and `Dark` values. Keep sample app controls aligned with this API.

`MonacoExtensionScriptPath` is optional and resolved under `MonacoAssetsPath`. It allows app-owned Monaco extensions (snippets/providers/config) without changing the package internals.

The WebView2 renderer should remain lazy. Do not instantiate WebView2 in native mode or before it is needed.

## Native Renderer

The native renderer walks Markdig objects and creates WinUI elements. Keep native rendering polished and predictable:

- Paragraphs use `RichTextBlock`.
- Headings use `TextBlock` and `MarkdownTheme.HeadingSizes`.
- Lists use `MarkdownListPanel`.
- Task list items render as disabled/read-only `CheckBox` controls.
- Code blocks use themed code colors and `NativeSyntaxHighlighter`.
- Tables should preserve header styling and theme borders.
- Images should respect `MarkdownTheme.MaxImageWidth`.

When adding support for another Markdown extension, update both the native visitor and `MarkdownRenderPlanner` so Auto mode knows whether native rendering is sufficient.

## WebView2 Renderer

The WebView2 renderer converts Markdig HTML into a self-contained shell and injects CSS from `MarkdownTheme`.

Keep native and WebView2 themes visually consistent. When adding theme properties, apply them in both:

- Native WinUI elements in `NativeMarkdownVisitor`
- CSS variables and rules in `MarkdownTheme.ToCss()`

Link clicks should continue to route through `window.chrome.webview.postMessage` and `MarkdownLinkEventArgs`.

## Themes

Built-in themes currently include:

- `MarkdownTheme.System`
- `MarkdownTheme.WinUILight`
- `MarkdownTheme.WinUIDark`
- `MarkdownTheme.GitHubLight`
- `MarkdownTheme.GitHubDark`

`MarkdownTheme.Light` and `MarkdownTheme.Dark` are aliases for WinUI light/dark.

Theme changes should be reflected in both renderers, including background, foreground, links, code, code borders, code corner radius, quotes, blockquote background, tables, table header background, rules, typography, spacing, heading weights, and image sizing.

## Sample App

The sample app is a control playground:

- Left: Markdown input
- Center: live preview
- Right: property panel
- Top: visibility toggles for input, preview, and properties

Keep the sample useful for manual QA. If a new public property or major theme knob is added, expose it in the right panel when practical.

Monaco assets are intentionally bundled in the sample app only under `samples/WinUI.Markdown.Sample/Assets/monaco` and refreshed from npm with `npm run sync-monaco` in that sample folder.

The sample enables markdown-focused Monaco enhancements using `samples/WinUI.Markdown.Sample/Assets/monaco/extensions/markdown.sample.js` via `MonacoExtensionScriptPath`.

The sample uses debounced theme application for sliders and text inputs. Preserve that pattern for controls that can fire rapidly.

## Coding Style

- Prefer existing patterns in this repo.
- Keep public API small and deliberate.
- Use nullable annotations.
- Use ASCII in source files unless the file already clearly needs Unicode.
- Avoid broad refactors while adding focused renderer support.
- Add comments only where they clarify non-obvious behavior.

## Testing

Current tests are intentionally lightweight because WinUI object construction can require a UI/runtime context. Prefer pure API/analyzer tests for normal unit tests.

Good future test targets:

- Auto mode fallback analysis.
- Theme CSS generation.
- Public event args and dependency property defaults.
- Native renderer mapping tests once a proper WinUI test harness is added.

Before handing off changes, run build and tests. Run pack for package metadata or NuGet-related changes.
