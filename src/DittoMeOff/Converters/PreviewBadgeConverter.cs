using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using DittoMeOff.Models;
using DittoMeOff.Services;

namespace DittoMeOff.Converters;

/// <summary>
/// Gets the preview badge text for display in the header
/// </summary>
public class PreviewBadgeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ClipboardItem item)
            return "Preview";

        if (item.FormatType != ContentFormatType.PlainText)
        {
            return ContentFormatDetector.GetFormatDisplayName(item.FormatType);
        }

        return "Text";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
