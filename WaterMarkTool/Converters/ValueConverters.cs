using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WaterMarkTool.Models;

namespace WaterMarkTool.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is bool b && b;
        if (parameter?.ToString() == "Inverse")
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is Visibility.Visible;
    }
}

public class PatternDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WatermarkPattern pattern)
        {
            return value?.ToString() ?? string.Empty;
        }

        return pattern switch
        {
            WatermarkPattern.Tile => "平铺模式",
            WatermarkPattern.Single => "单个水印",
            WatermarkPattern.Custom => "自定义数量",
            _ => pattern.ToString()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class PositionDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not WatermarkPosition position)
        {
            return value?.ToString() ?? string.Empty;
        }

        return position switch
        {
            WatermarkPosition.Center => "居中",
            WatermarkPosition.TopLeft => "左上角",
            WatermarkPosition.TopRight => "右上角",
            WatermarkPosition.BottomLeft => "左下角",
            WatermarkPosition.BottomRight => "右下角",
            _ => position.ToString()
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class OpacityPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return $"{Math.Round(d * 100)}%";
        }

        return "0%";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class RotationDisplayConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return $"{Math.Round(d)}°";
        }

        return "0°";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ColorToBrushConverter : IValueConverter
{
    public static readonly ColorToBrushConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Windows.Media.Color color)
        {
            return new SolidColorBrush(color);
        }

        return System.Windows.Media.Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ColorToHexConverter : IValueConverter
{
    public static readonly ColorToHexConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Windows.Media.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        return "#000000";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text)
        {
            return System.Windows.Media.Colors.Black;
        }

        try
        {
            var normalized = text.Trim();
            if (!normalized.StartsWith('#'))
            {
                normalized = "#" + normalized;
            }

            return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(normalized)!;
        }
        catch
        {
            return System.Windows.Media.Colors.Black;
        }
    }
}

public class EnumDescriptionConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            WatermarkPattern.Tile => "平铺模式",
            WatermarkPattern.Single => "单个水印",
            WatermarkPattern.Custom => "自定义数量",
            WatermarkPosition.Center => "居中",
            WatermarkPosition.TopLeft => "左上角",
            WatermarkPosition.TopRight => "右上角",
            WatermarkPosition.BottomLeft => "左下角",
            WatermarkPosition.BottomRight => "右下角",
            _ => value?.ToString() ?? string.Empty
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
