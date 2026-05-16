using System.Globalization;
using System.Windows.Data;

namespace DiskAnalyzer.WPF.Converters;

[ValueConversion(typeof(double), typeof(double))]
public class PercentToWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double percent)
            return Math.Max(0, Math.Min(100, percent));
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
