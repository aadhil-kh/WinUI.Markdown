# Changelog

All notable changes to this project will be documented in this file.

This project uses semantic versioning while the package API is stable enough to do so. Versions below `1.0.0` may still include breaking API refinements.

## 0.3.0 - Dual Pane Monaco Editor

- Added `MarkdownViewMode` with `PreviewOnly` (default) and `DualPane` modes.
- Added dual-pane editor + preview layout to `MarkdownView`.
- Added Monaco editor integration options: `MonacoAssetsPath`, `MonacoExtensionScriptPath`, `EditorLanguage`, and `MonacoTheme`.
- Added `MonacoEditorTheme` enum with `System`, `Light`, and `Dark` values.
- Added sample app controls for view mode, Monaco assets path, and Monaco theme.
- Added sample scripts and npm metadata to sync Monaco assets into sample app `Assets/monaco`.
- Added API tests for new enums and public Monaco extension script property.
- Improved dual-pane robustness by avoiding hard-fail behavior when Monaco assets are not available.

## 0.2.0 - Markdown Fidelity

- Added configurable heading bottom borders and GitHub-style H1/H2 borders.
- Added `MarkdownTheme.Dracula`.
- Added theme-aware native syntax highlighting colors.
- Improved GitHub light/dark defaults for typography, spacing, image sizing, tables, task lists, and heading dividers.
- Improved dark-theme code block contrast for WinUI Dark, GitHub Dark, and Dracula.
- Improved WebView2 task-list checkbox styling and nested list marker styling.
- Improved native list rendering with WinUI task checkboxes, shaped unordered bullets, and roman/alpha nested ordered markers.
- Added native rendering for footnotes and definition lists.
- Fixed native rendering of reference link definition metadata.
- Improved native inline code alignment and spacing parity with WebView2 rendering.
- Improved native table sizing, striping, scrolling, and compact cell spacing.
- Updated the sample playground with Dracula, heading border, and table alternate-row controls.

## 0.1.0 - Initial Preview

- Added `MarkdownView` WinUI control.
- Added `RenderMode.Auto`, `RenderMode.Native`, and `RenderMode.WebView2`.
- Added lazy WebView2 renderer with Markdig HTML rendering.
- Added native renderer for common Markdown blocks and inlines.
- Added native support for headings, paragraphs, emphasis, links, lists, task lists, blockquotes, tables, images, code, and horizontal rules.
- Added shared theming model with WinUI and GitHub light/dark themes.
- Added sample playground app.
