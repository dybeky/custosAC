using System.Globalization;
using System.Windows.Data;

namespace CustosAC.WPF.Converters;

public class ProgressToWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 &&
            values[0] is double progress &&
            values[1] is double containerWidth)
        {
            return progress * containerWidth;
        }

        // Fallback for single value (backwards compatibility)
        if (values.Length >= 1 && values[0] is int intProgress)
        {
            return (intProgress / 100.0) * 280; // Default width
        }

        return 0.0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
