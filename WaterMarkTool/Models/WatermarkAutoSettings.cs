using System.Drawing;
using MediaColor = System.Windows.Media.Color;

namespace WaterMarkTool.Models;

public sealed class WatermarkAutoSettings
{
    public required MediaColor Color { get; init; }
    public double Opacity { get; init; }
    public double Size { get; init; }
    public bool IsBold { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public readonly record struct ImageBackgroundStats(double Luminance, double Variance, int Width, int Height);
