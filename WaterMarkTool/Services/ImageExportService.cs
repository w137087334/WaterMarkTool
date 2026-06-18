using System.Drawing;
using System.IO;
using WaterMarkTool.Models;
using WaterMarkTool.Services;

namespace WaterMarkTool.Services;

public static class ImageExportService
{
    public static string GetSaveFilter(ExportSettings settings)
    {
        return settings.Format switch
        {
            ExportFormat.Jpeg => "JPEG 图片|*.jpg;*.jpeg",
            ExportFormat.Png => "PNG 图片|*.png",
            _ => "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp;*.tiff;*.tif;*.avif"
        };
    }

    public static string GetDefaultExtension(WatermarkImageItem item, ExportSettings settings)
    {
        if (settings.Format == ExportFormat.Png)
        {
            return ".png";
        }

        if (settings.Format == ExportFormat.Jpeg)
        {
            return ".jpg";
        }

        return ImageSharpLoader.NormalizeOutputExtension(
            string.IsNullOrEmpty(item.Source.OriginalExtension) ? ".png" : item.Source.OriginalExtension);
    }

    public static void SaveItem(WatermarkImageItem item, string outputPath, ExportSettings settings, WatermarkSettings watermarkSettings, WatermarkRenderContext context)
    {
        if (ShouldStreamAnimatedGif(item, settings, outputPath))
        {
            FolderBatchProcessor.ExportAnimatedGif(item.Source, watermarkSettings, context, outputPath);
            return;
        }

        var document = item.Watermarked ?? item.Source;
        ImageSharpLoader.SaveDocument(document, outputPath, settings);
    }

    public static void SaveItem(WatermarkImageItem item, Stream outputStream, ExportSettings settings, WatermarkSettings watermarkSettings, WatermarkRenderContext context)
    {
        if (ShouldStreamAnimatedGif(item, settings, null))
        {
            FolderBatchProcessor.ExportAnimatedGif(item.Source, watermarkSettings, context, outputStream);
            return;
        }

        var document = item.Watermarked ?? item.Source;
        ImageSharpLoader.SaveDocument(document, outputStream, settings);
    }

    private static bool ShouldStreamAnimatedGif(WatermarkImageItem item, ExportSettings settings, string? outputPath)
    {
        if (!item.Source.IsAnimated)
        {
            return false;
        }

        var ext = ImageSharpLoader.ResolveOutputExtension(item.Source.OriginalExtension, settings.Format, outputPath ?? string.Empty);
        return ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    public static void SaveBitmap(Bitmap bitmap, string outputPath, ExportSettings settings, string originalExtension = ".png")
    {
        var extension = ImageSharpLoader.ResolveOutputExtension(originalExtension, settings.Format, outputPath);
        ImageSharpLoader.SaveBitmap(bitmap, outputPath, extension, settings);
    }
}
