using System.Drawing;

using System.IO;

using SixLabors.ImageSharp;

using SixLabors.ImageSharp.Formats;

using SixLabors.ImageSharp.Formats.Gif;

using SixLabors.ImageSharp.Formats.Jpeg;

using SixLabors.ImageSharp.Metadata.Profiles.Exif;

using SixLabors.ImageSharp.PixelFormats;

using SixLabors.ImageSharp.Processing.Processors.Quantization;

using WaterMarkTool.Models;



namespace WaterMarkTool.Services;



public static class ImageSharpLoader

{

    public static ImageDocument Load(string path)

    {

        var extension = Path.GetExtension(path).ToLowerInvariant();

        using var stream = File.OpenRead(path);

        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);

        var format = image.Metadata.DecodedImageFormat;



        if (image.Frames.Count > 1 && extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))

        {

            return LoadAnimatedGif(image, path, extension, format);

        }



        var bitmap = IsGif(extension)
            ? PixelConverter.GifFrameToGdiBitmap(image, 0)
            : ToGdiBitmap(image);

        return ImageDocument.FromBitmap(bitmap, path, extension);

    }



    public static ImageDocument LoadFromBitmap(Bitmap bitmap, string path = "", string extension = ".png")

    {

        return ImageDocument.FromBitmap(bitmap, path, extension);

    }



    private static ImageDocument LoadAnimatedGif(

        SixLabors.ImageSharp.Image<Rgba32> image,

        string path,

        string extension,

        IImageFormat? format,

        bool preserveMetadata = true)

    {

        var exifProfile = preserveMetadata ? image.Metadata.ExifProfile?.DeepClone() : null;

        var frameCount = image.Frames.Count;

        var frames = new List<Bitmap>(frameCount);

        var delays = new List<int>(frameCount);



        for (var i = 0; i < frameCount; i++)

        {

            var delay = image.Frames[i].Metadata.GetGifMetadata().FrameDelay;

            delays.Add(delay > 0 ? delay * 10 : 100);

        }



        // Convert frame-by-frame, exporting each ImageSharp frame so its memory is released as we

        // go. Without this, the fully decoded animation (all frames) and all GDI bitmaps would be

        // held simultaneously, doubling peak memory and causing failures on large GIFs.

        // Pixels that ImageSharp composes as the GIF background color (e.g. regions cleared by

        // RestoreToBackground disposal) should be treated as transparent and flattened to white,

        // matching how typical viewers/browsers render them.

        var backgroundColor = ResolveGifBackgroundColor(image);



        for (var i = 0; i < frameCount; i++)

        {

            if (image.Frames.Count > 1)

            {

                using var single = image.Frames.ExportFrame(0);

                frames.Add(PixelConverter.GifFrameToGdiBitmap(single, 0, backgroundColor));

            }

            else

            {

                frames.Add(PixelConverter.GifFrameToGdiBitmap(image, 0, backgroundColor));

            }

        }



        // The decoded frame buffers are now idle in ImageSharp's pooled allocator. Release them

        // back to the OS so the (often large) pool does not sit alongside the GDI bitmaps during

        // the subsequent watermark/export steps, which is where peak memory matters most.

        SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.ReleaseRetainedResources();



        return ImageDocument.FromFrames(frames, delays, path, extension, exifProfile, format);

    }



    private static Rgba32? ResolveGifBackgroundColor(SixLabors.ImageSharp.Image<Rgba32> image)

    {

        var meta = image.Metadata.GetGifMetadata();

        var palette = meta.GlobalColorTable;

        if (palette is not { Length: > 0 } table)

        {

            return null;

        }



        var index = meta.BackgroundColorIndex;

        if (index >= table.Length)

        {

            return null;

        }



        return table.Span[index].ToPixel<Rgba32>();

    }



    public static Bitmap ToGdiBitmap(SixLabors.ImageSharp.Image<Rgba32> image)

    {

        return PixelConverter.ToGdiBitmap(image);

    }



    private static bool IsGif(string extension) =>
        extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);



    public static void SaveDocument(ImageDocument document, string outputPath, ExportSettings exportSettings)

    {

        if (document.IsAnimated && document.OriginalExtension.Equals(".gif", StringComparison.OrdinalIgnoreCase))

        {

            SaveAnimatedGif(document, outputPath);

            return;

        }



        var frame = document.GetFirstFrame();

        var extension = ResolveOutputExtension(document.OriginalExtension, exportSettings.Format, outputPath);

        SaveBitmap(frame, outputPath, extension, exportSettings, document);

    }



    public static void SaveDocument(ImageDocument document, Stream outputStream, ExportSettings exportSettings)

    {

        if (document.IsAnimated && document.OriginalExtension.Equals(".gif", StringComparison.OrdinalIgnoreCase))

        {

            SaveAnimatedGif(document, outputStream);

            return;

        }



        var frame = document.GetFirstFrame();

        var extension = ResolveOutputExtension(document.OriginalExtension, exportSettings.Format, string.Empty);

        SaveBitmap(frame, outputStream, extension, exportSettings, document);

    }



    public static string ResolveOutputExtension(string originalExtension, ExportFormat format, string outputPath)

    {

        if (format == ExportFormat.Png)

        {

            return ".png";

        }



        if (format == ExportFormat.Jpeg)

        {

            return ".jpg";

        }



        var fromPath = Path.GetExtension(outputPath);

        var extension = !string.IsNullOrEmpty(fromPath)

            ? fromPath.ToLowerInvariant()

            : string.IsNullOrEmpty(originalExtension) ? ".png" : originalExtension.ToLowerInvariant();



        return NormalizeOutputExtension(extension);

    }



    public static string NormalizeOutputExtension(string extension)

    {

        return extension.Equals(".avif", StringComparison.OrdinalIgnoreCase) ? ".png" : extension;

    }



    public static void SaveBitmap(Bitmap bitmap, string outputPath, string extension, ExportSettings settings, ImageDocument? sourceDoc = null)

    {

        using var image = PrepareImageForSave(bitmap, settings, sourceDoc);

        WriteImage(image, extension, settings, outputPath);

    }



    public static void SaveBitmap(Bitmap bitmap, Stream outputStream, string extension, ExportSettings settings, ImageDocument? sourceDoc = null)

    {

        using var image = PrepareImageForSave(bitmap, settings, sourceDoc);

        WriteImage(image, extension, settings, outputStream);

    }



    private static SixLabors.ImageSharp.Image<Rgba32> PrepareImageForSave(Bitmap bitmap, ExportSettings settings, ImageDocument? sourceDoc)

    {

        var image = FromGdiBitmap(bitmap);

        if (settings.PreserveMetadata && sourceDoc?.SourceExifProfile != null)

        {

            CopyExifProfile(sourceDoc.SourceExifProfile, image);

        }



        return image;

    }



    private static void WriteImage(SixLabors.ImageSharp.Image<Rgba32> image, string extension, ExportSettings settings, string outputPath)

    {

        switch (extension)

        {

            case ".jpg":

            case ".jpeg":

                image.Save(outputPath, new JpegEncoder { Quality = settings.JpegQuality });

                break;

            case ".webp":

                image.SaveAsWebp(outputPath);

                break;

            case ".tiff":

            case ".tif":

                image.SaveAsTiff(outputPath);

                break;

            case ".bmp":

                image.SaveAsBmp(outputPath);

                break;

            case ".gif":

                image.SaveAsGif(outputPath, CreateGifEncoder());

                break;

            default:

                image.SaveAsPng(outputPath);

                break;

        }

    }



    private static void WriteImage(SixLabors.ImageSharp.Image<Rgba32> image, string extension, ExportSettings settings, Stream outputStream)

    {

        switch (extension)

        {

            case ".jpg":

            case ".jpeg":

                image.Save(outputStream, new JpegEncoder { Quality = settings.JpegQuality });

                break;

            case ".webp":

                image.SaveAsWebp(outputStream);

                break;

            case ".tiff":

            case ".tif":

                image.SaveAsTiff(outputStream);

                break;

            case ".bmp":

                image.SaveAsBmp(outputStream);

                break;

            case ".gif":

                image.SaveAsGif(outputStream, CreateGifEncoder());

                break;

            default:

                image.SaveAsPng(outputStream);

                break;

        }

    }



    private static void SaveAnimatedGif(ImageDocument document, string outputPath)

    {

        using var animated = BuildAnimatedGifImage(document);

        animated?.SaveAsGif(outputPath, CreateGifEncoder());

    }



    private static void SaveAnimatedGif(ImageDocument document, Stream outputStream)

    {

        using var animated = BuildAnimatedGifImage(document);

        animated?.SaveAsGif(outputStream, CreateGifEncoder());

    }



    // Per-frame local palettes with the Wu quantizer and no dithering. The default global-palette

    // encoding can drop exact colors (e.g. pure white turning into a pink-ish tone) on complex,

    // many-frame animations; local palettes keep each frame's real colors intact.

    private static GifEncoder CreateGifEncoder() => new()

    {

        ColorTableMode = GifColorTableMode.Local,

        Quantizer = new WuQuantizer(new QuantizerOptions { Dither = null })

    };



    private static SixLabors.ImageSharp.Image<Rgba32>? BuildAnimatedGifImage(ImageDocument document)

    {

        return BuildAnimatedGif(document.FrameCount, i => (new Bitmap(document.GetFrame(i)), document.FrameDelaysMs[i]));

    }



    // Saves an animated GIF by pulling one watermarked frame at a time from the factory and

    // disposing each GDI bitmap right after it is encoded. This keeps the full set of watermarked

    // frames from being held in memory simultaneously, which is essential for large animations.

    public static void SaveAnimatedGifStreaming(int frameCount, Func<int, (Bitmap Frame, int DelayMs)> frameFactory, string outputPath)

    {

        using var animated = BuildAnimatedGif(frameCount, frameFactory);

        animated?.SaveAsGif(outputPath, CreateGifEncoder());

    }



    public static void SaveAnimatedGifStreaming(int frameCount, Func<int, (Bitmap Frame, int DelayMs)> frameFactory, Stream outputStream)

    {

        using var animated = BuildAnimatedGif(frameCount, frameFactory);

        animated?.SaveAsGif(outputStream, CreateGifEncoder());

    }



    private static SixLabors.ImageSharp.Image<Rgba32>? BuildAnimatedGif(int frameCount, Func<int, (Bitmap Frame, int DelayMs)> frameFactory)

    {

        SixLabors.ImageSharp.Image<Rgba32>? animated = null;

        try

        {

            for (var i = 0; i < frameCount; i++)

            {

                var (frame, delayMs) = frameFactory(i);

                try

                {

                    using var frameImage = FromGdiBitmap(frame);

                    if (animated == null)

                    {

                        animated = frameImage.Clone();

                        ApplyGifFrameMeta(animated.Frames.RootFrame, delayMs);

                    }

                    else

                    {

                        animated.Frames.AddFrame(frameImage.Frames.RootFrame);

                        ApplyGifFrameMeta(animated.Frames[i], delayMs);

                    }

                }

                finally

                {

                    frame.Dispose();

                }

            }



            return animated;

        }

        catch

        {

            animated?.Dispose();

            throw;

        }

    }



    private static void ApplyGifFrameMeta(SixLabors.ImageSharp.ImageFrame<Rgba32> frame, int delayMs)

    {

        var meta = frame.Metadata.GetGifMetadata();

        meta.FrameDelay = Math.Max(1, delayMs / 10);

        meta.DisposalMethod = GifDisposalMethod.RestoreToBackground;

    }



    private static SixLabors.ImageSharp.Image<Rgba32> FromGdiBitmap(Bitmap bitmap)

    {

        return PixelConverter.FromGdiBitmap(bitmap);

    }



    private static void CopyExifProfile(ExifProfile source, SixLabors.ImageSharp.Image target)

    {

        try

        {

            target.Metadata.ExifProfile = source;

        }

        catch

        {

            // best effort

        }

    }



    public static ImageDocument LoadWithMetadata(string path, bool preserveMetadata = true)

    {

        var extension = Path.GetExtension(path).ToLowerInvariant();

        using var stream = File.OpenRead(path);

        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);

        var format = image.Metadata.DecodedImageFormat;



        if (image.Frames.Count > 1 && extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))

        {

            return LoadAnimatedGif(image, path, extension, format, preserveMetadata);

        }



        var exifProfile = preserveMetadata ? image.Metadata.ExifProfile?.DeepClone() : null;

        var bitmap = IsGif(extension)
            ? PixelConverter.GifFrameToGdiBitmap(image, 0)
            : ToGdiBitmap(image);

        var doc = ImageDocument.FromBitmap(bitmap, path, extension);

        doc.SourceExifProfile = exifProfile;

        doc.SourceFormat = format;

        return doc;

    }

}


