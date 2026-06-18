using MediaColor = System.Windows.Media.Color;

namespace WaterMarkTool.Models;

public static class WatermarkSettingsMapper
{
    public static WatermarkSettingsDto ToDto(WatermarkSettings settings)
    {
        return new WatermarkSettingsDto
        {
            Text = settings.Text,
            Pattern = settings.Pattern,
            WatermarkCount = settings.WatermarkCount,
            Position = settings.Position,
            FontFamily = settings.FontFamily,
            IsBold = settings.IsBold,
            IsItalic = settings.IsItalic,
            Color = ColorToHex(settings.Color),
            Opacity = settings.Opacity,
            Spacing = settings.Spacing,
            Size = settings.Size,
            Rotation = settings.Rotation,
            TextOutlineEnabled = settings.TextOutlineEnabled,
            OutlineWidth = settings.OutlineWidth,
            OutlineColor = ColorToHex(settings.OutlineColor),
            TextShadowEnabled = settings.TextShadowEnabled,
            ShadowOffset = settings.ShadowOffset,
            TextBackgroundEnabled = settings.TextBackgroundEnabled,
            TextBackgroundOpacity = settings.TextBackgroundOpacity,
            UseCustomPosition = settings.UseCustomPosition,
            CustomOffsetX = settings.CustomOffsetX,
            CustomOffsetY = settings.CustomOffsetY,
            OverlayMode = settings.ImageOverlay.Mode,
            LogoPath = settings.ImageOverlay.LogoPath,
            QrContent = settings.ImageOverlay.QrContent,
            OverlayOpacity = settings.ImageOverlay.Opacity,
            OverlaySizeScale = settings.ImageOverlay.SizeScale,
            OverlayPosition = settings.ImageOverlay.Position,
            OverlayRotation = settings.ImageOverlay.Rotation,
            OverlayMarginPercent = settings.ImageOverlay.MarginPercent,
            OverlayUseCustomPosition = settings.ImageOverlay.UseCustomPosition,
            OverlayCustomOffsetX = settings.ImageOverlay.CustomOffsetX,
            OverlayCustomOffsetY = settings.ImageOverlay.CustomOffsetY
        };
    }

    public static void ApplyDto(WatermarkSettings settings, WatermarkSettingsDto dto)
    {
        settings.Text = dto.Text;
        settings.Pattern = dto.Pattern;
        settings.WatermarkCount = dto.WatermarkCount;
        settings.Position = dto.Position;
        settings.FontFamily = dto.FontFamily;
        settings.IsBold = dto.IsBold;
        settings.IsItalic = dto.IsItalic;
        settings.Color = HexToColor(dto.Color);
        settings.Opacity = dto.Opacity;
        settings.Spacing = dto.Spacing;
        settings.Size = dto.Size;
        settings.Rotation = dto.Rotation;
        settings.TextOutlineEnabled = dto.TextOutlineEnabled;
        settings.OutlineWidth = dto.OutlineWidth;
        settings.OutlineColor = HexToColor(dto.OutlineColor);
        settings.TextShadowEnabled = dto.TextShadowEnabled;
        settings.ShadowOffset = dto.ShadowOffset;
        settings.TextBackgroundEnabled = dto.TextBackgroundEnabled;
        settings.TextBackgroundOpacity = dto.TextBackgroundOpacity;
        settings.UseCustomPosition = dto.UseCustomPosition;
        settings.CustomOffsetX = dto.CustomOffsetX;
        settings.CustomOffsetY = dto.CustomOffsetY;

        settings.ImageOverlay.Mode = dto.OverlayMode;
        settings.ImageOverlay.LogoPath = dto.LogoPath;
        settings.ImageOverlay.QrContent = dto.QrContent;
        settings.ImageOverlay.Opacity = dto.OverlayOpacity;
        settings.ImageOverlay.SizeScale = dto.OverlaySizeScale;
        settings.ImageOverlay.Position = dto.OverlayPosition;
        settings.ImageOverlay.Rotation = dto.OverlayRotation;
        settings.ImageOverlay.MarginPercent = dto.OverlayMarginPercent;
        settings.ImageOverlay.UseCustomPosition = dto.OverlayUseCustomPosition;
        settings.ImageOverlay.CustomOffsetX = dto.OverlayCustomOffsetX;
        settings.ImageOverlay.CustomOffsetY = dto.OverlayCustomOffsetY;
    }

    public static string ColorToHex(MediaColor color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static MediaColor HexToColor(string hex)
    {
        try
        {
            var normalized = hex.Trim();
            if (!normalized.StartsWith('#'))
            {
                normalized = "#" + normalized;
            }

            return (MediaColor)System.Windows.Media.ColorConverter.ConvertFromString(normalized)!;
        }
        catch
        {
            return MediaColor.FromRgb(0, 0, 0);
        }
    }
}
