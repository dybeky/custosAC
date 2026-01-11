using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CustosAC.WPF.Converters;

/// <summary>
/// Converts boolean/integer values to Visibility.
/// True/non-zero = Visible, False/zero = Collapsed.
/// </summary>
public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isVisible = value switch
        {
            bool b => b,
            int i => i > 0,
            long l => l > 0,
            double d => d > 0,
            _ => value != null
        };

        // Parameter can invert the logic
        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            isVisible = !isVisible;
        }

        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}
