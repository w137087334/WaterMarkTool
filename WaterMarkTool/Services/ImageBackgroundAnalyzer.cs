using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using WaterMarkTool.Models;
using MediaColor = System.Windows.Media.Color;

namespace WaterMarkTool.Services;

public static class ImageBackgroundAnalyzer
{
    private const int SampleSize = 48;

    public static WatermarkAutoSettings Analyze(Bitmap bitmap)
    {
        return CreateSettings(ComputeStats(bitmap));
    }

    public static WatermarkAutoSettings AnalyzeAverage(IEnumerable<Bitmap> bitmaps)
    {
        var statsList = bitmaps.Select(ComputeStats).ToList();
        if (statsList.Count == 0)
        {
            return CreateSettings(new ImageBackgroundStats(0.75, 0.02, 1920, 1080));
        }

        var avgLuminance = statsList.Average(s => s.Luminance);
        var avgVariance = statsList.Average(s => s.Variance);
        var maxWidth = statsList.Max(s => s.Width);
        var maxHeight = statsList.Max(s => s.Height);
        return CreateSettings(new ImageBackgroundStats(avgLuminance, avgVariance, maxWidth, maxHeight));
    }

    private static ImageBackgroundStats ComputeStats(Bitmap bitmap)
    {
        using var sample = new Bitmap(SampleSize, SampleSize, PixelFormat.Format24bppRgb);
        using (var graphics = Graphics.FromImage(sample))
        {
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            graphics.DrawImage(bitmap, 0, 0, SampleSize, SampleSize);
        }

        var rect = new Rectangle(0, 0, SampleSize, SampleSize);
        var data = sample.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

        try
        {
            var bytes = new byte[data.Stride * data.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            double sumLuminance = 0;
            double sumLuminanceSquared = 0;
            var count = SampleSize * SampleSize;

            for (var y = 0; y < SampleSize; y++)
            {
                var row = y * data.Stride;
                for (var x = 0; x < SampleSize; x++)
                {
                    var index = row + x * 3;
                    var luminance = GetLuminance(bytes[index + 2], bytes[index + 1], bytes[index]);
                    sumLuminance += luminance;
                    sumLuminanceSquared += luminance * luminance;
                }
            }

            var mean = sumLuminance / count;
            var variance = Math.Max(0, sumLuminanceSquared / count - mean * mean);
            return new ImageBackgroundStats(mean, variance, bitmap.Width, bitmap.Height);
        }
        finally
        {
            sample.UnlockBits(data);
        }
    }

    private static double GetLuminance(byte r, byte g, byte b)
    {
        return (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
    }

    private static WatermarkAutoSettings CreateSettings(ImageBackgroundStats stats)
    {
        var isLightBackground = stats.Luminance >= 0.55;
        var isDarkBackground = stats.Luminance <= 0.4;
        var isBusyBackground = stats.Variance >= 0.035;

        MediaColor color;
        double opacity;
        var isBold = false;
        string tone;

        if (isLightBackground)
        {
            color = MediaColor.FromRgb(30, 41, 59);
            opacity = isBusyBackground ? 0.32 : 0.26;
            isBold = stats.Luminance >= 0.72;
            tone = "浅色背景";
        }
        else if (isDarkBackground)
        {
            color = MediaColor.FromRgb(241, 245, 249);
            opacity = isBusyBackground ? 0.28 : 0.22;
            tone = "深色背景";
        }
        else
        {
            color = MediaColor.FromRgb(51, 65, 85);
            opacity = isBusyBackground ? 0.34 : 0.28;
            tone = "中间色调背景";
        }

        var diagonal = Math.Sqrt(stats.Width * (double)stats.Width + stats.Height * stats.Height);
        var size = diagonal switch
        {
            < 900 => 0.85,
            > 2200 => 1.15,
            _ => 1.0
        };

        var summary = $"{tone}，已设置{(isLightBackground ? "深色" : isDarkBackground ? "浅色" : "对比色")}水印";
        if (isBusyBackground)
        {
            summary += "，并提高不透明度以增强可读性";
        }

        return new WatermarkAutoSettings
        {
            Color = color,
            Opacity = opacity,
            Size = size,
            IsBold = isBold,
            Summary = summary,
            OutlineColor = SuggestOutlineColor(stats)
        };
    }

    public static MediaColor SuggestOutlineColor(ImageBackgroundStats stats)
    {
        return stats.Luminance >= 0.55
            ? MediaColor.FromRgb(255, 255, 255)
            : MediaColor.FromRgb(0, 0, 0);
    }
}
