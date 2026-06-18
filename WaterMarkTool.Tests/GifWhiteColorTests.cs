using System.Drawing;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using WaterMarkTool.Models;
using WaterMarkTool.Services;
using Xunit;
using Xunit.Abstractions;
using GdiColor = System.Drawing.Color;
using IsColor = SixLabors.ImageSharp.Color;

namespace WaterMarkTool.Tests;

public class GifWhiteColorTests
{
    private readonly ITestOutputHelper _output;

    public GifWhiteColorTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void FullPipeline_WhiteGif_StaysWhite()
    {
        var tempIn = Path.Combine(Path.GetTempPath(), $"wm_in_{Guid.NewGuid():N}.gif");
        var tempOut = Path.Combine(Path.GetTempPath(), $"wm_out_{Guid.NewGuid():N}.gif");

        // build a white animated gif
        using (var gif = new Image<Rgba32>(64, 64, new Rgba32(255, 255, 255, 255)))
        {
            using var frame2 = new Image<Rgba32>(64, 64, new Rgba32(255, 255, 255, 255));
            frame2[3, 3] = new Rgba32(0, 0, 0, 255);
            gif.Frames.AddFrame(frame2.Frames.RootFrame);
            gif.SaveAsGif(tempIn);
        }

        var settings = new WatermarkSettings();
        var context = WatermarkRenderContext.Default;
        var export = new ExportSettings();

        using (var source = ImageSharpLoader.LoadWithMetadata(tempIn, export.PreserveMetadata))
        {
            _output.WriteLine($"loaded frames={source.FrameCount} animated={source.IsAnimated} ext={source.OriginalExtension}");
            using var watermarked = FolderBatchProcessor.ApplyWatermarkToDocument(source, settings, context);

            // inspect the bitmap corner before save (should be white)
            using var f0 = new Bitmap(watermarked.GetFrame(0));
            var beforePx = f0.GetPixel(60, 60);
            _output.WriteLine($"before-save corner pixel = R{beforePx.R} G{beforePx.G} B{beforePx.B} A{beforePx.A}");

            ImageSharpLoader.SaveDocument(watermarked, tempOut, export);
        }

        using var reloaded = SixLabors.ImageSharp.Image.Load<Rgba32>(tempOut);
        var px = reloaded[60, 60];
        _output.WriteLine($"after-save corner pixel = R{px.R} G{px.G} B{px.B} A{px.A}");

        File.Delete(tempIn);
        File.Delete(tempOut);

        Assert.True(px.R > 240 && px.G > 240 && px.B > 240, $"corner not white: R{px.R} G{px.G} B{px.B}");
    }

    [Fact]
    public void FullPipeline_TransparentColorfulGif_WhiteStaysWhite()
    {
        var tempIn = Path.Combine(Path.GetTempPath(), $"wm_tin_{Guid.NewGuid():N}.gif");
        var tempOut = Path.Combine(Path.GetTempPath(), $"wm_tout_{Guid.NewGuid():N}.gif");

        using (var gif = new Image<Rgba32>(64, 64)) // transparent bg
        {
            // colorful content + white area
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    if (x < 20) gif[x, y] = new Rgba32((byte)(x * 8), (byte)(y * 3), 50, 255);
                    else if (x < 44) gif[x, y] = new Rgba32(255, 255, 255, 255); // white band
                    // x>=44 stays transparent
                }

            using var frame2 = new Image<Rgba32>(64, 64);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    if (x < 20) frame2[x, y] = new Rgba32(10, (byte)(x * 7), (byte)(y * 4), 255);
                    else if (x < 44) frame2[x, y] = new Rgba32(255, 255, 255, 255);
                }
            gif.Frames.AddFrame(frame2.Frames.RootFrame);
            gif.SaveAsGif(tempIn);
        }

        var settings = new WatermarkSettings();
        var export = new ExportSettings();

        using (var source = ImageSharpLoader.LoadWithMetadata(tempIn, export.PreserveMetadata))
        {
            using var watermarked = FolderBatchProcessor.ApplyWatermarkToDocument(source, settings, WatermarkRenderContext.Default);
            ImageSharpLoader.SaveDocument(watermarked, tempOut, export);
        }

        using var reloaded = SixLabors.ImageSharp.Image.Load<Rgba32>(tempOut);
        // sample several points in the white band (x 24..40) across both frames
        for (int f = 0; f < reloaded.Frames.Count; f++)
        {
            var frame = reloaded.Frames[f];
            var px = frame[30, 55]; // white band, away from watermark text near center
            _output.WriteLine($"frame{f} whiteband(30,55) = R{px.R} G{px.G} B{px.B} A{px.A}");
        }

        var sample = reloaded.Frames[0][30, 55];
        File.Delete(tempIn);
        File.Delete(tempOut);
        Assert.True(sample.R > 230 && sample.G > 230 && sample.B > 230,
            $"white band turned non-white: R{sample.R} G{sample.G} B{sample.B} A{sample.A}");
    }

    [Fact]
    public void FullPipeline_NoisyMultiFrameGif_WhiteRegionSurvivesInLaterFrames()
    {
        var tempIn = Path.Combine(Path.GetTempPath(), $"wm_nin_{Guid.NewGuid():N}.gif");
        var tempOut = Path.Combine(Path.GetTempPath(), $"wm_nout_{Guid.NewGuid():N}.gif");

        var rnd = new Random(7);
        using (var gif = new Image<Rgba32>(128, 128))
        {
            FillNoisyWithWhiteCorner(gif, rnd);
            using var f1 = new Image<Rgba32>(128, 128);
            FillNoisyWithWhiteCorner(f1, rnd);
            gif.Frames.AddFrame(f1.Frames.RootFrame);
            gif.SaveAsGif(tempIn);
        }

        var settings = new WatermarkSettings();
        var export = new ExportSettings();

        using (var source = ImageSharpLoader.LoadWithMetadata(tempIn, export.PreserveMetadata))
        {
            using var watermarked = FolderBatchProcessor.ApplyWatermarkToDocument(source, settings, WatermarkRenderContext.Default);
            ImageSharpLoader.SaveDocument(watermarked, tempOut, export);
        }

        using var reloaded = SixLabors.ImageSharp.Image.Load<Rgba32>(tempOut);
        for (int f = 0; f < reloaded.Frames.Count; f++)
        {
            var px = reloaded.Frames[f][120, 120];
            _output.WriteLine($"frame{f} white-corner(120,120) = R{px.R} G{px.G} B{px.B} A{px.A}");
        }

        var lastFrame = reloaded.Frames[reloaded.Frames.Count - 1][120, 120];
        File.Delete(tempIn);
        File.Delete(tempOut);
        Assert.True(lastFrame.R > 230 && lastFrame.G > 230 && lastFrame.B > 230,
            $"white corner in last frame corrupted: R{lastFrame.R} G{lastFrame.G} B{lastFrame.B} A{lastFrame.A}");
    }

    private static void FillNoisyWithWhiteCorner(Image<Rgba32> img, Random rnd)
    {
        for (int y = 0; y < img.Height; y++)
            for (int x = 0; x < img.Width; x++)
            {
                if (x >= 100 && y >= 100)
                    img[x, y] = new Rgba32(255, 255, 255, 255);
                else
                    img[x, y] = new Rgba32((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256), 255);
            }
    }
}
