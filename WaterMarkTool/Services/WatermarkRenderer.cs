using System.Drawing;
using WaterMarkTool.Models;

namespace WaterMarkTool.Services;

public static class WatermarkRenderer
{
    public static Bitmap ApplyWatermark(Bitmap source, WatermarkSettings settings, WatermarkRenderContext? context = null)
    {
        return WatermarkCompositor.ApplyWatermark(source, settings, context);
    }
}
