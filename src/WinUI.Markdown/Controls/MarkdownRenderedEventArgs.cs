namespace WinUI.Markdown.Controls;

public sealed class MarkdownRenderedEventArgs : EventArgs
{
    public MarkdownRenderedEventArgs(RenderMode requestedRenderMode, RenderMode actualRenderMode)
    {
        RequestedRenderMode = requestedRenderMode;
        ActualRenderMode = actualRenderMode;
    }

    public RenderMode RenderMode => ActualRenderMode;

    public RenderMode RequestedRenderMode { get; }

    public RenderMode ActualRenderMode { get; }
}
