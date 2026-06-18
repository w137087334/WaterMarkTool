namespace WaterMarkTool.Models;

public sealed class WatermarkPreset
{
    public string Name { get; set; } = string.Empty;
    public WatermarkSettingsDto Settings { get; set; } = new();
}

public sealed class WatermarkSettingsDto
{
    public string Text { get; set; } = "水印文字";
    public WatermarkPattern Pattern { get; set; } = WatermarkPattern.Tile;
    public int WatermarkCount { get; set; } = 4;
    public WatermarkPosition Position { get; set; } = WatermarkPosition.Center;
    public string FontFamily { get; set; } = "黑体";
    public bool IsBold { get; set; }
    public bool IsItalic { get; set; }
    public string Color { get; set; } = "#000000";
    public double Opacity { get; set; } = 0.3;
    public double Spacing { get; set; } = 2;
    public double Size { get; set; } = 1;
    public double Rotation { get; set; } = 45;

    public bool TextOutlineEnabled { get; set; }
    public double OutlineWidth { get; set; } = 2;
    public string OutlineColor { get; set; } = "#FFFFFF";
    public bool TextShadowEnabled { get; set; }
    public double ShadowOffset { get; set; } = 2;
    public bool TextBackgroundEnabled { get; set; }
    public double TextBackgroundOpacity { get; set; } = 0.4;

    public bool UseCustomPosition { get; set; }
    public double CustomOffsetX { get; set; } = 0.5;
    public double CustomOffsetY { get; set; } = 0.5;

    public ImageOverlayMode OverlayMode { get; set; } = ImageOverlayMode.None;
    public string LogoPath { get; set; } = string.Empty;
    public string QrContent { get; set; } = string.Empty;
    public double OverlayOpacity { get; set; } = 0.8;
    public double OverlaySizeScale { get; set; } = 1.0;
    public WatermarkPosition OverlayPosition { get; set; } = WatermarkPosition.BottomRight;
    public double OverlayRotation { get; set; }
    public double OverlayMarginPercent { get; set; } = 0.1;
    public bool OverlayUseCustomPosition { get; set; }
    public double OverlayCustomOffsetX { get; set; } = 0.85;
    public double OverlayCustomOffsetY { get; set; } = 0.9;
}
