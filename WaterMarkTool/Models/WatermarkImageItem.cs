using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using WaterMarkTool.Services;

namespace WaterMarkTool.Models;

public class WatermarkImageItem : INotifyPropertyChanged
{
    private BitmapImage? _previewImage;
    private BitmapImage? _sourcePreviewImage;

    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required ImageDocument Source { get; set; }
    public ImageDocument? Watermarked { get; set; }

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        private set => SetField(ref _previewImage, value);
    }

    public BitmapImage? SourcePreviewImage
    {
        get => _sourcePreviewImage;
        private set => SetField(ref _sourcePreviewImage, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdatePreview()
    {
        var watermarked = Watermarked?.GetFirstFrame() ?? Source.GetFirstFrame();
        var source = Source.GetFirstFrame();
        PreviewImage = ImageHelper.ToBitmapImage(watermarked);
        SourcePreviewImage = ImageHelper.ToBitmapImage(source);
    }

    public void Dispose()
    {
        Source.Dispose();
        Watermarked?.Dispose();
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }
}

public static class ImageHelper
{
    private static readonly string[] SupportedExtensions =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico", ".tiff", ".tif", ".avif"
    ];

    private static readonly string[] UnsupportedExtensions =
    [
        ".heic", ".heif"
    ];

    public static bool IsSupported(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (UnsupportedExtensions.Contains(ext))
        {
            return false;
        }

        return SupportedExtensions.Contains(ext);
    }

    public static bool IsHeic(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".heic" or ".heif";
    }

    public static BitmapImage ToBitmapImage(Bitmap bitmap)
    {
        using var memory = new MemoryStream();
        bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
        memory.Position = 0;

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = memory;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static WatermarkImageItem CloneItem(WatermarkImageItem item)
    {
        var sourceFrames = new List<Bitmap>();
        var delays = new List<int>();
        for (var i = 0; i < item.Source.FrameCount; i++)
        {
            sourceFrames.Add(new Bitmap(item.Source.GetFrame(i)));
            delays.Add(item.Source.FrameDelaysMs[i]);
        }

        var source = ImageDocument.FromFrames(sourceFrames, delays, item.FilePath, item.Source.OriginalExtension);

        ImageDocument? watermarked = null;
        if (item.Watermarked != null)
        {
            var wmFrames = new List<Bitmap>();
            var wmDelays = new List<int>();
            for (var i = 0; i < item.Watermarked.FrameCount; i++)
            {
                wmFrames.Add(new Bitmap(item.Watermarked.GetFrame(i)));
                wmDelays.Add(item.Watermarked.FrameDelaysMs[i]);
            }

            watermarked = ImageDocument.FromFrames(wmFrames, wmDelays, item.FilePath, item.Source.OriginalExtension);
        }

        var clone = new WatermarkImageItem
        {
            FilePath = item.FilePath,
            FileName = item.FileName,
            Source = source,
            Watermarked = watermarked
        };
        clone.UpdatePreview();
        return clone;
    }

    public static Bitmap FromBitmapSource(System.Windows.Media.Imaging.BitmapSource source)
    {
        using var stream = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        encoder.Save(stream);
        stream.Position = 0;
        return new Bitmap(stream);
    }
}
