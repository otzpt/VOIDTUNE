using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace VOIDTUNE.WinUI.Converters;

/// <summary>Converts a "#RRGGBB" hex string to a SolidColorBrush.</summary>
public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && hex.StartsWith('#') && hex.Length == 7)
        {
            byte r = System.Convert.ToByte(hex.Substring(1, 2), 16);
            byte g = System.Convert.ToByte(hex.Substring(3, 2), 16);
            byte b = System.Convert.ToByte(hex.Substring(5, 2), 16);
            return new SolidColorBrush(Color.FromArgb(255, r, g, b));
        }
        return new SolidColorBrush(Color.FromArgb(255, 136, 136, 136));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>True -> Visible, False -> Collapsed.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => (value is bool b && b) ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Microsoft.UI.Xaml.Visibility v && v == Microsoft.UI.Xaml.Visibility.Visible;
}

/// <summary>Boolean NOT — handy for IsEnabled="{x:Bind Busy, Converter=...}".</summary>
public sealed class BoolNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => !(value is bool b && b);

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => !(value is bool b && b);
}
