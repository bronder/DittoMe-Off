using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DittoMeOff.Converters;

public class SearchHighlightConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue)
            return new TextBlock();

        string previewText = values[0]?.ToString() ?? "";
        string searchText = values[1]?.ToString() ?? "";

        var textBlock = new TextBlock();
        textBlock.TextWrapping = TextWrapping.Wrap;
        textBlock.MaxHeight = 60;
        textBlock.FontSize = 13;
        textBlock.FontWeight = FontWeights.Normal;
        textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

        if (string.IsNullOrEmpty(searchText))
        {
            textBlock.Text = previewText;
            return textBlock;
        }

        string lowerText = previewText.ToLower();
        string lowerSearch = searchText.ToLower();
        int index = 0;

        while (index < lowerText.Length)
        {
            int matchIndex = lowerText.IndexOf(lowerSearch, index, StringComparison.Ordinal);
            
            if (matchIndex == -1)
            {
                // No more matches, add remaining text
                if (index < previewText.Length)
                {
                    var run = new Run(previewText.Substring(index));
                    run.Foreground = Application.Current.Resources["TextBrush"] as Brush ?? Brushes.White;
                    textBlock.Inlines.Add(run);
                }
                break;
            }

            // Add text before match
            if (matchIndex > index)
            {
                var run = new Run(previewText.Substring(index, matchIndex - index));
                run.Foreground = Application.Current.Resources["TextBrush"] as Brush ?? Brushes.White;
                textBlock.Inlines.Add(run);
            }

            // Add highlighted match
            var highlightRun = new Run(previewText.Substring(matchIndex, searchText.Length));
            highlightRun.Foreground = Application.Current.Resources["AccentBrush"] as Brush ?? Brushes.Yellow;
            highlightRun.FontWeight = FontWeights.Bold;
            highlightRun.Background = new SolidColorBrush(Color.FromArgb(60, 255, 200, 0)); // Semi-transparent yellow
            textBlock.Inlines.Add(highlightRun);

            index = matchIndex + searchText.Length;
        }

        return textBlock;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
