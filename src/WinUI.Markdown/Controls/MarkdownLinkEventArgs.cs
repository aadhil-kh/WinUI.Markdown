namespace WinUI.Markdown.Controls;

public sealed class MarkdownLinkEventArgs : EventArgs
{
    public MarkdownLinkEventArgs(string url, string? title = null)
    {
        Url = url;
        Title = title;
    }

    public string Url { get; }

    public string? Title { get; }

    public bool Handled { get; set; }
}
