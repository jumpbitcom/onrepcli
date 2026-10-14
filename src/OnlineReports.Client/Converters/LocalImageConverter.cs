// Loads only locally cached image files for display in Avalonia controls.
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace OnlineReports.Client.Converters;

public sealed class LocalImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path))
        {
            return null;
        }

        return new Bitmap(path);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
