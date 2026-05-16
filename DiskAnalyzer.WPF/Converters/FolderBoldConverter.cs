using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DiskAnalyzer.Core.Models;

namespace DiskAnalyzer.WPF.Converters;

[ValueConversion(typeof(FileSystemNode), typeof(FontWeight))]
public class FolderBoldConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is FolderNode ? FontWeights.Bold : FontWeights.Normal;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}
