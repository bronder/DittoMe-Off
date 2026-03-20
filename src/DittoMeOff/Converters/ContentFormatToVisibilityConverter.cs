using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DittoMeOff.Models;

namespace DittoMeOff.Converters;

/// <summary>
/// Converts a ClipboardItem to visibility for the preview panel - shows when item is selected
/// </summary>
public class ContentFormatToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        // Show preview for any selected ClipboardItem
        if (value is ClipboardItem)
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a ClipboardItem to visibility for image preview
/// </summary>
public class ImagePreviewVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ClipboardItem item && item.ContentType == ContentType.Image && item.ImageSource != null)
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a ClipboardItem to visibility for text/code preview
/// </summary>
public class TextPreviewVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ClipboardItem item && item.ContentType != ContentType.Image)
            return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a ClipboardItem to a preview document for display
/// </summary>
public class ContentToPreviewConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ClipboardItem item)
            return "";

        // For plain text, return truncated content
        if (item.FormatType == ContentFormatType.PlainText)
        {
            var content = item.Content ?? "";
            return content.Length > 500 ? content.Substring(0, 500) + "..." : content;
        }

        // For formatted content, return up to 1000 chars for preview
        var formattedContent = item.Content ?? "";
        return formattedContent.Length > 1000 ? formattedContent.Substring(0, 1000) + "\n\n[...truncated...]" : formattedContent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
