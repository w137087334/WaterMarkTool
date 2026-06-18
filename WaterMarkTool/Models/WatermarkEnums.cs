namespace WaterMarkTool.Models;

public enum WatermarkPattern
{
    Tile,
    Single,
    Custom
}

public enum WatermarkPosition
{
    Center,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public enum PreviewMode
{
    Watermarked,
    Original,
    SideBySide,
    Slider
}
