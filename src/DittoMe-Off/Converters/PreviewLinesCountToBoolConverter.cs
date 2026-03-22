using System.Globalization;
using System.Windows.Data;

namespace DittoMeOff.Converters;

public class PreviewLinesCountToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int previewLinesCount && parameter is string paramString && int.TryParse(paramString, out int targetValue))
        {
            return previewLinesCount == targetValue;
        }
        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string paramString && int.TryParse(paramString, out int targetValue))
        {
            return targetValue;
        }
        return Binding.DoNothing;
    }
}
