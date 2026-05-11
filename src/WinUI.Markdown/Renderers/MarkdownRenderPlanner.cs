using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using WinUI.Markdown.Controls;

namespace WinUI.Markdown.Renderers;

internal static class MarkdownRenderPlanner
{
    public static MarkdownRenderPlan Resolve(RenderMode requestedMode, MarkdownDocument document, bool allowWebView2Fallback)
    {
        if (requestedMode is RenderMode.Native or RenderMode.WebView2)
        {
            return new MarkdownRenderPlan(requestedMode, string.Empty);
        }

        var fallbackReason = GetFallbackReason(document);
        if (string.IsNullOrEmpty(fallbackReason))
        {
            return new MarkdownRenderPlan(RenderMode.Native, string.Empty);
        }

        return allowWebView2Fallback
            ? new MarkdownRenderPlan(RenderMode.WebView2, fallbackReason)
            : new MarkdownRenderPlan(RenderMode.Native, fallbackReason);
    }

    private static string GetFallbackReason(MarkdownDocument document)
    {
        foreach (var item in document.Descendants())
        {
            if (IsUnsupported(item))
            {
                return item switch
                {
                    HtmlBlock => "Raw HTML block",
                    HtmlInline => "Raw HTML inline",
                    _ => item.GetType().Name
                };
            }
        }

        return string.Empty;
    }

    private static bool IsUnsupported(MarkdownObject item)
    {
        var type = item.GetType();
        var name = type.Name;
        var ns = type.Namespace ?? string.Empty;

        if (item is HtmlBlock or HtmlInline)
        {
            return true;
        }

        if (ns.Contains("Footnotes", StringComparison.Ordinal) ||
            ns.Contains("Mathematics", StringComparison.Ordinal) ||
            ns.Contains("DefinitionLists", StringComparison.Ordinal) ||
            ns.Contains("Figures", StringComparison.Ordinal) ||
            ns.Contains("Diagrams", StringComparison.Ordinal))
        {
            return true;
        }

        return name.Contains("Math", StringComparison.Ordinal) ||
               name.Contains("Footnote", StringComparison.Ordinal);
    }
}

internal sealed record MarkdownRenderPlan(RenderMode ActualRenderMode, string FallbackReason);
