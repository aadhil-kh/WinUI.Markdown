using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUI.Markdown.Themes;

namespace WinUI.Markdown.Controls;

public sealed class MarkdownListPanel : StackPanel
{
    public MarkdownListPanel()
    {
        Spacing = 4;
    }

    public bool IsOrdered { get; set; }

    public int StartNumber { get; set; } = 1;

    public int NestingLevel { get; set; }

    public MarkdownTheme? Theme { get; set; }

    internal void AddItem(FrameworkElement content, bool? isTask = null)
    {
        var index = Children.Count + StartNumber;
        var marker = IsOrdered ? $"{index}." : BulletForLevel(NestingLevel);
        var row = new Grid
        {
            ColumnSpacing = 8,
            Margin = new Thickness(Math.Min(NestingLevel * 18, 72), 1, 0, 1)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(isTask.HasValue ? 24 : 32) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        UIElement markerElement = isTask.HasValue
            ? new CheckBox
            {
                IsChecked = isTask.Value,
                IsEnabled = false,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0)
            }
            : new TextBlock
            {
                Text = marker,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 2, 0, 0),
                Foreground = Theme?.Foreground
            };

        row.Children.Add(markerElement);

        Grid.SetColumn(content, 1);
        row.Children.Add(content);
        Children.Add(row);
    }

    private static string BulletForLevel(int level)
    {
        return (level % 3) switch
        {
            1 => "-",
            2 => "+",
            _ => "*"
        };
    }
}
