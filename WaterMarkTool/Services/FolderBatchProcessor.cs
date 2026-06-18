using System.Drawing;
using System.IO;
using WaterMarkTool.Models;
using WaterMarkTool.Services;

namespace WaterMarkTool.Services;

public sealed class FolderBatchProcessor
{
    public async Task<FolderBatchResult> ProcessAsync(
        FolderBatchOptions options,
        WatermarkSettings settings,
        ExportSettings exportSettings,
        string customText,
        IProgress<FolderBatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var files = CollectFiles(options);
        var result = new FolderBatchResult { Total = files.Count };
        var errors = new List<string>();

        Directory.CreateDirectory(options.OutputFolder);

        await Task.Run(() =>
        {
            for (var i = 0; i < files.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var file = files[i];
                progress?.Report(new FolderBatchProgress { Current = i + 1, Total = files.Count, CurrentFile = file });

                try
                {
                    ProcessFile(file, i + 1, files.Count, options, settings, exportSettings, customText);
                    result.Succeeded++;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    errors.Add($"{file}: {ex.Message}");
                }
            }
        }, cancellationToken);

        if (errors.Count > 0)
        {
            var logPath = Path.Combine(options.OutputFolder, "errors.log");
            await File.WriteAllLinesAsync(logPath, errors, cancellationToken);
            result.ErrorLogPath = logPath;
        }

        return result;
    }

    private static void ProcessFile(
        string file,
        int fileIndex,
        int fileTotal,
        FolderBatchOptions options,
        WatermarkSettings settings,
        ExportSettings exportSettings,
        string customText)
    {
        if (!ImageHelper.IsSupported(file))
        {
            throw new InvalidOperationException("不支持的格式");
        }

        using var sourceDoc = ImageSharpLoader.LoadWithMetadata(file, exportSettings.PreserveMetadata);
        var relative = Path.GetRelativePath(options.InputFolder, file);
        var outputPath = Path.Combine(options.OutputFolder, relative);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var extension = ImageSharpLoader.ResolveOutputExtension(sourceDoc.OriginalExtension, exportSettings.Format, outputPath);
        outputPath = Path.ChangeExtension(outputPath, extension);

        var context = new WatermarkRenderContext
        {
            FileName = Path.GetFileName(file),
            Index = fileIndex,
            Total = fileTotal,
            CustomText = customText
        };

        if (sourceDoc.IsAnimated && extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            ExportAnimatedGif(sourceDoc, settings, context, outputPath);
            return;
        }

        using var watermarked = ApplyWatermarkToDocument(sourceDoc, settings, context);
        ImageSharpLoader.SaveDocument(watermarked, outputPath, exportSettings);
    }

    public static ImageDocument ApplyWatermarkToDocument(ImageDocument source, WatermarkSettings settings, WatermarkRenderContext context)
    {
        return source.CloneWatermarked((frame, index) =>
            WatermarkRenderer.ApplyWatermark(frame, settings, BuildFrameContext(source, context, index)));
    }

    // Watermarks only the first frame for a lightweight preview document. The full set of
    // watermarked frames is large for long animations and is only needed during export, so it is
    // produced on demand (streaming) rather than kept resident in memory.
    public static ImageDocument ApplyWatermarkPreview(ImageDocument source, WatermarkSettings settings, WatermarkRenderContext context)
    {
        var frame = WatermarkRenderer.ApplyWatermark(source.GetFrame(0), settings, BuildFrameContext(source, context, 0));
        var delay = source.FrameDelaysMs.Count > 0 ? source.FrameDelaysMs[0] : 100;
        return ImageDocument.FromFrames(new[] { frame }, new[] { delay }, source.SourcePath, source.OriginalExtension);
    }

    public static void ExportAnimatedGif(ImageDocument source, WatermarkSettings settings, WatermarkRenderContext context, string outputPath)
    {
        ImageSharpLoader.SaveAnimatedGifStreaming(
            source.FrameCount,
            index => (WatermarkRenderer.ApplyWatermark(source.GetFrame(index), settings, BuildFrameContext(source, context, index)), source.FrameDelaysMs[index]),
            outputPath);
    }

    public static void ExportAnimatedGif(ImageDocument source, WatermarkSettings settings, WatermarkRenderContext context, Stream outputStream)
    {
        ImageSharpLoader.SaveAnimatedGifStreaming(
            source.FrameCount,
            index => (WatermarkRenderer.ApplyWatermark(source.GetFrame(index), settings, BuildFrameContext(source, context, index)), source.FrameDelaysMs[index]),
            outputStream);
    }

    private static WatermarkRenderContext BuildFrameContext(ImageDocument source, WatermarkRenderContext context, int index)
    {
        return new WatermarkRenderContext
        {
            FileName = context.FileName,
            Index = context.Index,
            Total = context.Total,
            FrameIndex = index + 1,
            FrameTotal = source.FrameCount,
            CustomText = context.CustomText
        };
    }

    private static List<string> CollectFiles(FolderBatchOptions options)
    {
        var search = options.IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory
            .EnumerateFiles(options.InputFolder, "*.*", search)
            .Where(ImageHelper.IsSupported)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

public sealed class FolderBatchOptions
{
    public required string InputFolder { get; init; }
    public required string OutputFolder { get; init; }
    public bool IncludeSubfolders { get; init; }
}

public sealed class FolderBatchProgress
{
    public int Current { get; init; }
    public int Total { get; init; }
    public string CurrentFile { get; init; } = string.Empty;
}

public sealed class FolderBatchResult
{
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public string? ErrorLogPath { get; set; }
}
