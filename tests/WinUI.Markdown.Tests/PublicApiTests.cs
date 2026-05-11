using WinUI.Markdown.Controls;

namespace WinUI.Markdown.Tests;

public sealed class PublicApiTests
{
    [Xunit.Fact]
    public void RenderModeExposesNativeAndWebView2Modes()
    {
        var names = Enum.GetNames<RenderMode>();

        Xunit.Assert.Contains(nameof(RenderMode.Auto), names);
        Xunit.Assert.Contains(nameof(RenderMode.Native), names);
        Xunit.Assert.Contains(nameof(RenderMode.WebView2), names);
    }

    [Xunit.Fact]
    public void LinkEventArgsStoresUrlTitleAndHandledState()
    {
        var args = new MarkdownLinkEventArgs("https://example.com", "Example");

        Xunit.Assert.Equal("https://example.com", args.Url);
        Xunit.Assert.Equal("Example", args.Title);
        Xunit.Assert.False(args.Handled);
        args.Handled = true;
        Xunit.Assert.True(args.Handled);
    }
}
