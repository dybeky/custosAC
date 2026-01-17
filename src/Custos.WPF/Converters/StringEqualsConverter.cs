using System.Globalization;
using System.Windows.Data;

namespace Custos.WPF.Converters;

/// <summary>
/// Compares a string value with a parameter and returns "Selected" if they are equal.
/// Used for determining if a navigation item is currently selected.
/// Parameter can contain multiple values separated by '|' for OR matching.
/// </summary>
public class StringEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string str && parameter is string param)
        {
            // Support multiple values with '|' separator
            var values = param.Split('|');
            foreach (var val in values)
            {
                if (string.Equals(str, val.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    return "Selected";
                }
            }
        }
        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
