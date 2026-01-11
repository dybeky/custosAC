using System.Globalization;
using System.Windows.Data;

namespace CustosAC.WPF.Converters;

public class ProgressToWidthConverter : IValueConverter
{
    public double MaxWidth { get; set; } = 800; // Default max width

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int progress)
        {
            // Calculate width based on percentage (0-100)
            return (progress / 100.0) * MaxWidth;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
