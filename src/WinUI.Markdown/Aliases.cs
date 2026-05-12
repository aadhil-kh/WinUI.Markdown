// On Uno, bridge the Microsoft.UI.Xaml.Documents/Controls namespaces to our
// UnoRichText implementations via global using aliases.
// On Windows (WINDOWS_APP_SDK) the native WinUI types are used as-is.
#if !WINDOWS_APP_SDK
global using RichTextBlock    = LeXtudio.UI.Xaml.Controls.RichTextBlock;
global using InlineCollection = LeXtudio.UI.Xaml.Documents.InlineCollection;
global using Paragraph        = LeXtudio.UI.Xaml.Documents.Paragraph;
global using Run              = LeXtudio.UI.Xaml.Documents.Run;
global using Bold             = LeXtudio.UI.Xaml.Documents.Bold;
global using Italic           = LeXtudio.UI.Xaml.Documents.Italic;
global using Span             = LeXtudio.UI.Xaml.Documents.Span;
global using LineBreak        = LeXtudio.UI.Xaml.Documents.LineBreak;
global using InlineUIContainer = LeXtudio.UI.Xaml.Documents.InlineUIContainer;
global using Hyperlink        = LeXtudio.UI.Xaml.Documents.Hyperlink;
#endif
