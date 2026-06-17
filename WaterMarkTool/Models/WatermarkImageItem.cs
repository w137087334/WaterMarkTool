using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

namespace WaterMarkTool.Models;

public class WatermarkImageItem : INotifyPropertyChanged
{
    private BitmapImage? _previewImage;

    public required string FilePath { get; init; }
    public required string FileName { get; init; }
    public required Bitmap SourceBitmap { get; set; }
    public Bitmap? WatermarkedBitmap { get; set; }

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        private set => SetField(ref _previewImage, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpdatePreview()
    {
        var bitmap = WatermarkedBitmap ?? SourceBitmap;
        PreviewImage = ImageHelper.ToBitmapImage(bitmap);
    }

    public void Dispose()
    {
        SourceBitmap.Dispose();
        WatermarkedBitmap?.Dispose();
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
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp", ".ico"
    ];

    public static bool IsSupported(string path)
    {
        return SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
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

    public static Bitmap CloneBitmap(Bitmap source)
    {
        return new Bitmap(source);
    }

    public static WatermarkImageItem CloneItem(WatermarkImageItem item)
    {
        var source = CloneBitmap(item.SourceBitmap);
        var watermarked = item.WatermarkedBitmap != null ? CloneBitmap(item.WatermarkedBitmap) : null;
        var clone = new WatermarkImageItem
        {
            FilePath = item.FilePath,
            FileName = item.FileName,
            SourceBitmap = source,
            WatermarkedBitmap = watermarked
        };
        clone.UpdatePreview();
        return clone;
    }

    public static Bitmap FromBitmapSource(System.Windows.Media.Imaging.BitmapSource source)
    {
        using var stream = new MemoryStream();
        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
        encoder.Save(stream);
        stream.Position = 0;
        return new Bitmap(stream);
    }
}
