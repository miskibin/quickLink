using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace quickLink.Helpers
{
    /// <summary>
    /// Attached properties for a TextBlock that render fuzzy-match highlights as bold accent runs.
    /// Usage:
    ///   &lt;TextBlock helpers:TextHighlight.Text="{Binding DisplayTitle}"
    ///              helpers:TextHighlight.Indexes="{Binding TitleHighlights}" /&gt;
    /// </summary>
    public static class TextHighlight
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
                "Text", typeof(string), typeof(TextHighlight),
                new PropertyMetadata(null, OnChanged));

        public static readonly DependencyProperty IndexesProperty =
            DependencyProperty.RegisterAttached(
                "Indexes", typeof(int[]), typeof(TextHighlight),
                new PropertyMetadata(null, OnChanged));

        public static string? GetText(DependencyObject obj) => obj.GetValue(TextProperty) as string;
        public static void SetText(DependencyObject obj, string? value) => obj.SetValue(TextProperty, value);

        public static int[]? GetIndexes(DependencyObject obj) => obj.GetValue(IndexesProperty) as int[];
        public static void SetIndexes(DependencyObject obj, int[]? value) => obj.SetValue(IndexesProperty, value);

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBlock tb) return;

            var text = GetText(tb) ?? string.Empty;
            var indexes = GetIndexes(tb);

            tb.Inlines.Clear();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (indexes == null || indexes.Length == 0)
            {
                tb.Inlines.Add(new Run { Text = text });
                return;
            }

            var accentBrush = (Microsoft.UI.Xaml.Media.Brush?)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];

            int cursor = 0;
            int idx = 0;
            while (cursor < text.Length)
            {
                if (idx < indexes.Length && indexes[idx] == cursor)
                {
                    int runStart = cursor;
                    while (idx < indexes.Length && indexes[idx] == cursor && cursor < text.Length)
                    {
                        idx++;
                        cursor++;
                    }
                    var hl = new Run
                    {
                        Text = text.Substring(runStart, cursor - runStart),
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    };
                    if (accentBrush != null) hl.Foreground = accentBrush;
                    tb.Inlines.Add(hl);
                }
                else
                {
                    int nextHighlight = idx < indexes.Length ? indexes[idx] : text.Length;
                    if (nextHighlight < cursor) nextHighlight = text.Length;
                    int len = Math.Max(0, nextHighlight - cursor);
                    if (len == 0) { cursor++; continue; }
                    tb.Inlines.Add(new Run { Text = text.Substring(cursor, len) });
                    cursor += len;
                }
            }
        }
    }
}
