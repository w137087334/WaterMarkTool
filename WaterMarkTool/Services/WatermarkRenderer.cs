using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using WaterMarkTool.Models;

namespace WaterMarkTool.Services;

public static class WatermarkRenderer
{
    private static readonly Dictionary<string, string> FontMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["黑体"] = "SimHei",
        ["宋体"] = "SimSun",
        ["仿宋"] = "FangSong",
        ["楷体"] = "KaiTi",
        ["隶书"] = "LiSu",
        ["幼圆"] = "YouYuan",
        ["Arial"] = "Arial",
        ["Helvetica"] = "Arial",
        ["Tahoma"] = "Tahoma",
        ["Verdana"] = "Verdana",
        ["Georgia"] = "Georgia",
        ["Times New Roman"] = "Times New Roman"
    };

    public static Bitmap ApplyWatermark(Bitmap source, WatermarkSettings settings)
    {
        var result = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);

        DrawWatermark(graphics, result.Width, result.Height, settings);
        return result;
    }

    private static void DrawWatermark(Graphics graphics, int width, int height, WatermarkSettings settings)
    {
        var text = string.IsNullOrWhiteSpace(settings.Text) ? "水印文字" : settings.Text.Trim();
        var diagonal = Math.Sqrt(width * width + height * height);
        if (diagonal < 1)
        {
            diagonal = 1000;
        }

        var textSize = Math.Max(12, settings.Size * Math.Max(15, diagonal / 25));
        var fontFamily = ResolveFontFamily(settings.FontFamily);
        var fontStyle = FontStyle.Regular;
        if (settings.IsBold)
        {
            fontStyle |= FontStyle.Bold;
        }

        if (settings.IsItalic)
        {
            fontStyle |= FontStyle.Italic;
        }

        using var font = CreateFont(fontFamily, (float)textSize, fontStyle);
        var color = Color.FromArgb(
            (int)(settings.Opacity * 255),
            settings.Color.R,
            settings.Color.G,
            settings.Color.B);
        using var brush = new SolidBrush(color);

        var maxWidth = Math.Min(Math.Max(100, width * 0.8), 500);
        var lines = WrapText(graphics, text, font, maxWidth);
        var lineHeight = textSize * 1.2f;
        var totalHeight = lineHeight * lines.Count;

        if (settings.Pattern == WatermarkPattern.Tile)
        {
            DrawTilePattern(graphics, brush, font, lines, width, height, diagonal, textSize, lineHeight, totalHeight, settings.Spacing, settings.Rotation);
        }
        else
        {
            var positions = CalculatePositions(
                graphics,
                font,
                lines,
                width,
                height,
                settings.Pattern,
                settings.WatermarkCount,
                settings.Position,
                totalHeight,
                padding: Math.Min(width, height) * 0.1);

            foreach (var (x, y) in positions)
            {
                DrawSingleWatermark(graphics, brush, font, lines, x, y, lineHeight, textSize, settings.Rotation);
            }
        }
    }

    private static string ResolveFontFamily(string name)
    {
        if (FontMap.TryGetValue(name, out var mapped))
        {
            return mapped;
        }

        return name;
    }

    private static Font CreateFont(string family, float size, FontStyle style)
    {
        try
        {
            return new Font(family, size, style, GraphicsUnit.Pixel);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Pixel);
        }
    }

    private static List<string> WrapText(Graphics graphics, string text, Font font, double maxWidth)
    {
        maxWidth = Math.Max(50, maxWidth);
        var pattern = new Regex(@"([，。！？；：、,.!?;:\s]|[0-9\-\/年月日]+|[a-zA-Z]+|[\u4e00-\u9fa5]+|[^\u4e00-\u9fa5a-zA-Z0-9\s])", RegexOptions.Compiled);
        var segments = pattern.Matches(text).Select(m => m.Value).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (segments.Count == 0)
        {
            return ["水印文字"];
        }

        var lines = new List<string>();
        var currentLine = string.Empty;

        float Measure(string str) => graphics.MeasureString(str, font).Width;

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i].Trim();
            if (segment.Length == 0)
            {
                continue;
            }

            var nextSegment = i + 1 < segments.Count ? segments[i + 1].Trim() : string.Empty;
            var isPunctuation = IsPunctuation(segment);
            var isNextPunctuation = IsPunctuation(nextSegment);

            if (isPunctuation)
            {
                if (string.IsNullOrEmpty(currentLine) && lines.Count > 0)
                {
                    var lastLine = lines[^1];
                    var testLastLine = lastLine + segment;
                    if (Measure(testLastLine) <= maxWidth)
                    {
                        lines[^1] = testLastLine;
                    }
                    else
                    {
                        currentLine = segment;
                    }
                }
                else
                {
                    currentLine += segment;
                }

                continue;
            }

            var testLine = currentLine + segment + (isNextPunctuation ? nextSegment : string.Empty);
            if (Measure(testLine) <= maxWidth)
            {
                currentLine += segment;
                if (isNextPunctuation)
                {
                    currentLine += nextSegment;
                    i++;
                }
            }
            else if (!string.IsNullOrEmpty(currentLine))
            {
                if (!IsPunctuation(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = segment;
                    if (isNextPunctuation && Measure(segment + nextSegment) <= maxWidth)
                    {
                        currentLine += nextSegment;
                        i++;
                    }
                }
                else
                {
                    currentLine += segment;
                }
            }
            else if (segment.Length <= 4)
            {
                lines.Add(segment);
                currentLine = string.Empty;
            }
            else
            {
                var tempLine = string.Empty;
                foreach (var ch in segment)
                {
                    var testTemp = tempLine + ch;
                    if (Measure(testTemp) <= maxWidth)
                    {
                        tempLine = testTemp;
                    }
                    else if (!string.IsNullOrEmpty(tempLine))
                    {
                        lines.Add(tempLine);
                        tempLine = ch.ToString();
                    }
                    else
                    {
                        tempLine = ch.ToString();
                    }
                }

                currentLine = tempLine;
            }
        }

        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines.Count > 0 ? lines : ["水印文字"];
    }

    private static bool IsPunctuation(string value)
    {
        return Regex.IsMatch(value, @"^[，。！？；：、,.!?;:\s]$");
    }

    private static void DrawTilePattern(
        Graphics graphics,
        Brush brush,
        Font font,
        IReadOnlyList<string> lines,
        int width,
        int height,
        double diagonal,
        double textSize,
        double lineHeight,
        double totalHeight,
        double spacing,
        double rotation)
    {
        var state = graphics.Save();
        graphics.TranslateTransform(width / 2f, height / 2f);
        graphics.RotateTransform((float)rotation);

        var maxLineWidth = lines.Max(line => graphics.MeasureString(line, font).Width);
        var xStep = maxLineWidth + graphics.MeasureString("啊", font).Width;
        var yStep = spacing * (totalHeight + textSize);
        var rectWidth = diagonal;
        var rectHeight = diagonal;
        var startX = -rectWidth / 2;
        var startY = -rectHeight / 2;
        var cols = (int)Math.Ceiling(rectWidth / xStep);
        var rows = (int)Math.Ceiling(rectHeight / yStep);

        for (var i = 0; i <= cols; i++)
        {
            for (var j = 0; j <= rows; j++)
            {
                var x = (float)(startX + i * xStep);
                var y = (float)(startY + j * yStep);
                for (var index = 0; index < lines.Count; index++)
                {
                    var line = lines[index];
                    var lineY = y + (float)((index - (lines.Count - 1) / 2.0) * lineHeight);
                    var lineWidth = graphics.MeasureString(line, font).Width;
                    graphics.DrawString(line, font, brush, x - lineWidth / 2, lineY);
                }
            }
        }

        graphics.Restore(state);
    }

    private static List<(float X, float Y)> CalculatePositions(
        Graphics graphics,
        Font font,
        IReadOnlyList<string> lines,
        int width,
        int height,
        WatermarkPattern pattern,
        int count,
        WatermarkPosition position,
        double totalHeight,
        double padding)
    {
        var positions = new List<(float X, float Y)>();
        var maxLineWidth = lines.Max(line => graphics.MeasureString(line, font).Width);

        if (pattern == WatermarkPattern.Single || count == 1)
        {
            positions.Add(position switch
            {
                WatermarkPosition.TopLeft => ((float)(maxLineWidth / 2 + padding), (float)(totalHeight / 2 + padding)),
                WatermarkPosition.TopRight => ((float)(width - maxLineWidth / 2 - padding), (float)(totalHeight / 2 + padding)),
                WatermarkPosition.BottomLeft => ((float)(maxLineWidth / 2 + padding), (float)(height - totalHeight / 2 - padding)),
                WatermarkPosition.BottomRight => ((float)(width - maxLineWidth / 2 - padding), (float)(height - totalHeight / 2 - padding)),
                _ => (width / 2f, height / 2f)
            });
            return positions;
        }

        var cols = (int)Math.Ceiling(Math.Sqrt(count * width / (double)height));
        var rows = (int)Math.Ceiling(count / (double)cols);
        var xStep = (width - 2 * padding) / Math.Max(cols - 1, 1);
        var yStep = (height - 2 * padding) / Math.Max(rows - 1, 1);
        var currentCount = 0;

        for (var i = 0; i < cols && currentCount < count; i++)
        {
            for (var j = 0; j < rows && currentCount < count; j++)
            {
                positions.Add(((float)(padding + i * xStep), (float)(padding + j * yStep)));
                currentCount++;
            }
        }

        return positions;
    }

    private static void DrawSingleWatermark(
        Graphics graphics,
        Brush brush,
        Font font,
        IReadOnlyList<string> lines,
        float x,
        float y,
        double lineHeight,
        double textSize,
        double rotation)
    {
        var state = graphics.Save();
        graphics.TranslateTransform(x, y);
        graphics.RotateTransform((float)rotation);

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineY = (float)((index - (lines.Count - 1) / 2.0) * lineHeight);
            var lineWidth = graphics.MeasureString(line, font).Width;
            graphics.DrawString(line, font, brush, -lineWidth / 2, lineY);
        }

        graphics.Restore(state);
    }
}
