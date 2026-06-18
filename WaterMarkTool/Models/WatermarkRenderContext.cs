namespace WaterMarkTool.Models;

public sealed class WatermarkRenderContext
{
    public string FileName { get; init; } = string.Empty;
    public int Index { get; init; } = 1;
    public int Total { get; init; } = 1;
    public int FrameIndex { get; init; } = 1;
    public int FrameTotal { get; init; } = 1;
    public string CustomText { get; init; } = string.Empty;

    public static WatermarkRenderContext Default { get; } = new();
}
