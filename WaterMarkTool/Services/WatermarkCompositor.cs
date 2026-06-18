using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Text.RegularExpressions;
using WaterMarkTool.Models;

namespace WaterMarkTool.Services;

public static class WatermarkCompositor
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

    private static readonly Dictionary<string, Bitmap> LogoCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object LogoCacheLock = new();

    public static Bitmap ApplyWatermark(Bitmap source, WatermarkSettings settings, WatermarkRenderContext? context = null)
    {
        context ??= WatermarkRenderContext.Default;
        var result = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(result);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);

        DrawTextWatermark(graphics, result.Width, result.Height, settings, context);
        DrawImageOverlay(graphics, result.Width, result.Height, settings, context);
        return result;
    }

    private static void DrawTextWatermark(Graphics graphics, int width, int height, WatermarkSettings settings, WatermarkRenderContext context)
    {
        var text = WatermarkTextResolver.Resolve(settings.Text, context);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = "水印文字";
        }
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

        var maxWidth = Math.Min(Math.Max(100, width * 0.8), 500);
        var lines = WrapText(graphics, text, font, maxWidth);
        var lineHeight = textSize * 1.2f;
        var totalHeight = lineHeight * lines.Count;

        if (settings.Pattern == WatermarkPattern.Tile)
        {
            DrawTilePattern(graphics, font, lines, width, height, diagonal, textSize, lineHeight, totalHeight, settings);
        }
        else
        {
            var positions = CalculateTextPositions(
                graphics,
                font,
                lines,
                width,
                height,
                settings,
                totalHeight);

            foreach (var (x, y) in positions)
            {
                DrawStyledTextBlock(graphics, font, lines, x, y, lineHeight, textSize, settings, color);
            }
        }
    }

    private static void DrawImageOverlay(Graphics graphics, int width, int height, WatermarkSettings settings, WatermarkRenderContext context)
    {
        var overlay = settings.ImageOverlay;
        if (overlay.Mode == ImageOverlayMode.None)
        {
            return;
        }

        Bitmap? overlayBitmap = null;
        var ownsBitmap = false;

        try
        {
            if (overlay.Mode == ImageOverlayMode.Logo)
            {
                if (string.IsNullOrWhiteSpace(overlay.LogoPath) || !File.Exists(overlay.LogoPath))
                {
                    return;
                }

                overlayBitmap = LoadLogo(overlay.LogoPath);
                ownsBitmap = false;
            }
            else if (overlay.Mode == ImageOverlayMode.QrCode)
            {
                var content = WatermarkTextResolver.Resolve(overlay.QrContent, context);
                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }

                overlayBitmap = QrCodeGenerator.Generate(content);
                ownsBitmap = true;
            }

            if (overlayBitmap == null)
            {
                return;
            }

            var diagonal = Math.Sqrt(width * width + height * height);
            var targetSize = Math.Max(48, overlay.SizeScale * Math.Max(40, diagonal / 12));
            var scale = targetSize / Math.Max(overlayBitmap.Width, overlayBitmap.Height);
            var drawWidth = overlayBitmap.Width * scale;
            var drawHeight = overlayBitmap.Height * scale;

            var (centerX, centerY) = ResolveOverlayPosition(
                width,
                height,
                drawWidth,
                drawHeight,
                overlay);

            var state = graphics.Save();
            graphics.TranslateTransform(centerX, centerY);
            graphics.RotateTransform((float)overlay.Rotation);

            using var imageAttributes = new ImageAttributes();
            var matrix = new ColorMatrix { Matrix33 = (float)overlay.Opacity };
            imageAttributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            graphics.DrawImage(
                overlayBitmap,
                new Rectangle((int)(-drawWidth / 2), (int)(-drawHeight / 2), (int)drawWidth, (int)drawHeight),
                0,
                0,
                overlayBitmap.Width,
                overlayBitmap.Height,
                GraphicsUnit.Pixel,
                imageAttributes);

            graphics.Restore(state);
        }
        finally
        {
            if (ownsBitmap)
            {
                overlayBitmap?.Dispose();
            }
        }
    }

    private static Bitmap LoadLogo(string path)
    {
        lock (LogoCacheLock)
        {
            if (LogoCache.TryGetValue(path, out var cached))
            {
                return new Bitmap(cached);
            }

            var bitmap = new Bitmap(path);
            LogoCache[path] = bitmap;
            return new Bitmap(bitmap);
        }
    }

    public static void ClearLogoCache()
    {
        lock (LogoCacheLock)
        {
            foreach (var bitmap in LogoCache.Values)
            {
                bitmap.Dispose();
            }

            LogoCache.Clear();
        }
    }

    private static (float X, float Y) ResolveOverlayPosition(
        int width,
        int height,
        double drawWidth,
        double drawHeight,
        ImageOverlaySettings overlay)
    {
        if (overlay.UseCustomPosition)
        {
            return ((float)(overlay.CustomOffsetX * width), (float)(overlay.CustomOffsetY * height));
        }

        var padding = Math.Min(width, height) * overlay.MarginPercent;
        return overlay.Position switch
        {
            WatermarkPosition.TopLeft => ((float)(drawWidth / 2 + padding), (float)(drawHeight / 2 + padding)),
            WatermarkPosition.TopRight => ((float)(width - drawWidth / 2 - padding), (float)(drawHeight / 2 + padding)),
            WatermarkPosition.BottomLeft => ((float)(drawWidth / 2 + padding), (float)(height - drawHeight / 2 - padding)),
            WatermarkPosition.BottomRight => ((float)(width - drawWidth / 2 - padding), (float)(height - drawHeight / 2 - padding)),
            _ => (width / 2f, height / 2f)
        };
    }

    private static void DrawStyledTextBlock(
        Graphics graphics,
        Font font,
        IReadOnlyList<string> lines,
        float x,
        float y,
        double lineHeight,
        double textSize,
        WatermarkSettings settings,
        Color color)
    {
        var state = graphics.Save();
        graphics.TranslateTransform(x, y);
        graphics.RotateTransform((float)settings.Rotation);

        var bounds = GetTextBounds(graphics, font, lines, lineHeight);
        if (settings.TextBackgroundEnabled)
        {
            var bgColor = Color.FromArgb(
                (int)(settings.TextBackgroundOpacity * 255),
                0,
                0,
                0);
            using var bgBrush = new SolidBrush(bgColor);
            graphics.FillRectangle(bgBrush, bounds);
        }

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineY = (float)((index - (lines.Count - 1) / 2.0) * lineHeight);
            var lineWidth = graphics.MeasureString(line, font).Width;
            var drawX = -lineWidth / 2;
            DrawStyledLine(graphics, line, font, drawX, lineY, settings, color);
        }

        graphics.Restore(state);
    }

    private static RectangleF GetTextBounds(Graphics graphics, Font font, IReadOnlyList<string> lines, double lineHeight)
    {
        var maxWidth = lines.Max(line => graphics.MeasureString(line, font).Width);
        var totalHeight = (float)(lineHeight * lines.Count);
        return new RectangleF((float)(-maxWidth / 2 - 8), (float)(-totalHeight / 2 - 4), (float)(maxWidth + 16), totalHeight + 8);
    }

    private static void DrawStyledLine(
        Graphics graphics,
        string line,
        Font font,
        float x,
        float y,
        WatermarkSettings settings,
        Color color)
    {
        if (settings.TextShadowEnabled)
        {
            var shadowColor = Color.FromArgb((int)(color.A * 0.5), 0, 0, 0);
            using var shadowBrush = new SolidBrush(shadowColor);
            var offset = (float)settings.ShadowOffset;
            graphics.DrawString(line, font, shadowBrush, x + offset, y + offset);
        }

        if (settings.TextOutlineEnabled)
        {
            using var path = new GraphicsPath();
            path.AddString(line, font.FontFamily, (int)font.Style, font.Size, new PointF(x, y), StringFormat.GenericDefault);
            var outlineColor = Color.FromArgb(color.A, settings.OutlineColor.R, settings.OutlineColor.G, settings.OutlineColor.B);
            using var pen = new Pen(outlineColor, (float)settings.OutlineWidth) { LineJoin = LineJoin.Round };
            graphics.DrawPath(pen, path);
            using var fillBrush = new SolidBrush(color);
            graphics.FillPath(fillBrush, path);
        }
        else
        {
            using var brush = new SolidBrush(color);
            graphics.DrawString(line, font, brush, x, y);
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
        Font font,
        IReadOnlyList<string> lines,
        int width,
        int height,
        double diagonal,
        double textSize,
        double lineHeight,
        double totalHeight,
        WatermarkSettings settings)
    {
        var color = Color.FromArgb(
            (int)(settings.Opacity * 255),
            settings.Color.R,
            settings.Color.G,
            settings.Color.B);

        var state = graphics.Save();
        graphics.TranslateTransform(width / 2f, height / 2f);
        graphics.RotateTransform((float)settings.Rotation);

        var maxLineWidth = lines.Max(line => graphics.MeasureString(line, font).Width);
        var xStep = maxLineWidth + graphics.MeasureString("啊", font).Width;
        var yStep = settings.Spacing * (totalHeight + textSize);
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
                    DrawStyledLine(graphics, line, font, x - lineWidth / 2, lineY, settings, color);
                }
            }
        }

        graphics.Restore(state);
    }

    private static List<(float X, float Y)> CalculateTextPositions(
        Graphics graphics,
        Font font,
        IReadOnlyList<string> lines,
        int width,
        int height,
        WatermarkSettings settings,
        double totalHeight)
    {
        var positions = new List<(float X, float Y)>();
        var maxLineWidth = lines.Max(line => graphics.MeasureString(line, font).Width);
        var padding = Math.Min(width, height) * 0.1;

        if (settings.UseCustomPosition)
        {
            positions.Add(((float)(settings.CustomOffsetX * width), (float)(settings.CustomOffsetY * height)));
            return positions;
        }

        if (settings.Pattern == WatermarkPattern.Single || settings.WatermarkCount == 1)
        {
            positions.Add(settings.Position switch
            {
                WatermarkPosition.TopLeft => ((float)(maxLineWidth / 2 + padding), (float)(totalHeight / 2 + padding)),
                WatermarkPosition.TopRight => ((float)(width - maxLineWidth / 2 - padding), (float)(totalHeight / 2 + padding)),
                WatermarkPosition.BottomLeft => ((float)(maxLineWidth / 2 + padding), (float)(height - totalHeight / 2 - padding)),
                WatermarkPosition.BottomRight => ((float)(width - maxLineWidth / 2 - padding), (float)(height - totalHeight / 2 - padding)),
                _ => (width / 2f, height / 2f)
            });
            return positions;
        }

        var cols = (int)Math.Ceiling(Math.Sqrt(settings.WatermarkCount * width / (double)height));
        var rows = (int)Math.Ceiling(settings.WatermarkCount / (double)cols);
        var xStep = (width - 2 * padding) / Math.Max(cols - 1, 1);
        var yStep = (height - 2 * padding) / Math.Max(rows - 1, 1);
        var currentCount = 0;

        for (var i = 0; i < cols && currentCount < settings.WatermarkCount; i++)
        {
            for (var j = 0; j < rows && currentCount < settings.WatermarkCount; j++)
            {
                positions.Add(((float)(padding + i * xStep), (float)(padding + j * yStep)));
                currentCount++;
            }
        }

        return positions;
    }
}
