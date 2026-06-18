using System.Drawing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;

namespace WaterMarkTool.Models;

public sealed class ImageDocument : IDisposable
{
    private readonly List<Bitmap> _frames = [];
    private readonly List<int> _frameDelaysMs = [];

    public string SourcePath { get; init; } = string.Empty;
    public string OriginalExtension { get; init; } = ".png";
    public bool IsAnimated => _frames.Count > 1;
    public int FrameCount => _frames.Count;
    public IReadOnlyList<int> FrameDelaysMs => _frameDelaysMs;
    public IImageFormat? SourceFormat { get; set; }
    public ExifProfile? SourceExifProfile { get; set; }

    public static ImageDocument FromBitmap(Bitmap bitmap, string path = "", string extension = ".png")
    {
        var doc = new ImageDocument
        {
            SourcePath = path,
            OriginalExtension = extension
        };
        doc._frames.Add(new Bitmap(bitmap));
        doc._frameDelaysMs.Add(100);
        return doc;
    }

    public static ImageDocument FromFrames(
        IReadOnlyList<Bitmap> frames,
        IReadOnlyList<int> delaysMs,
        string path,
        string extension,
        ExifProfile? exifProfile = null,
        IImageFormat? format = null)
    {
        var doc = new ImageDocument
        {
            SourcePath = path,
            OriginalExtension = extension,
            SourceExifProfile = exifProfile,
            SourceFormat = format
        };

        for (var i = 0; i < frames.Count; i++)
        {
            doc._frames.Add(frames[i]);
            doc._frameDelaysMs.Add(i < delaysMs.Count ? delaysMs[i] : 100);
        }

        return doc;
    }

    public Bitmap GetFrame(int index)
    {
        if (index < 0 || index >= _frames.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _frames[index];
    }

    public Bitmap GetFirstFrame() => _frames[0];

    public ImageDocument CloneWatermarked(Func<Bitmap, int, Bitmap> watermarker)
    {
        var frames = new List<Bitmap>();
        var delays = new List<int>();

        for (var i = 0; i < _frames.Count; i++)
        {
            frames.Add(watermarker(_frames[i], i));
            delays.Add(_frameDelaysMs[i]);
        }

        return FromFrames(frames, delays, SourcePath, OriginalExtension, SourceExifProfile, SourceFormat);
    }

    public void Dispose()
    {
        foreach (var frame in _frames)
        {
            frame.Dispose();
        }

        _frames.Clear();
        SourceExifProfile = null;
    }
}
