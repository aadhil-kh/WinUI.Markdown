# Contributing

This project currently doesn't accept external contributions as it is in early development, but the following guidelines will be followed when that changes. If you have suggestions or want to contribute, please open an issue.

Thanks for helping improve `WinUI.Markdown`.

## Development Setup

Prerequisites:

- Windows 10 or later
- .NET 10 SDK
- Visual Studio with WinUI / Windows App SDK workloads, or the equivalent command-line tooling

From the repository root:

```powershell
dotnet restore MarkdownView.slnx
dotnet build MarkdownView.slnx -c Debug --no-restore
dotnet test tests\WinUI.Markdown.Tests\WinUI.Markdown.Tests.csproj -c Debug --no-restore --no-build
```

## Pull Requests

- Keep changes focused and scoped.
- Preserve lazy WebView2 creation. Native mode must not instantiate WebView2.
- Keep native and WebView2 theme behavior consistent.
- Update `MarkdownRenderPlanner` when native renderer support changes.
- Update the sample app when adding public control or theme properties.
- Add or update tests when behavior can be verified outside a WinUI UI runtime.

## Packaging Check

Before release-related changes are merged:

```powershell
dotnet pack src\WinUI.Markdown\MarkdownView.csproj -c Release --no-restore -o artifacts
```
