using System.Windows;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfSystemColors = System.Windows.SystemColors;

namespace WaterMarkTool.Services;

public static class ThemeManager
{
    private static readonly Dictionary<string, SolidColorBrush> ThemeBrushes = new();

    public static void Apply(bool isDark)
    {
        WpfApplication.Current.Resources["IsDarkTheme"] = isDark;

        SetBrush("AppBackgroundBrush", isDark ? "#0F172A" : "#F5F7FA");
        SetBrush("AppSurfaceBrush", isDark ? "#1E293B" : "#FFFFFF");
        SetBrush("AppInputBackgroundBrush", isDark ? "#0F172A" : "#FFFFFF");
        SetBrush("AppBorderBrush", isDark ? "#475569" : "#E2E8F0");
        SetBrush("AppControlBorderBrush", isDark ? "#94A3B8" : "#CBD5E1");
        SetBrush("AppTextBrush", isDark ? "#F1F5F9" : "#1E293B");
        SetBrush("AppSecondaryTextBrush", isDark ? "#94A3B8" : "#64748B");
        SetBrush("AppPrimaryBrush", isDark ? "#60A5FA" : "#3B82F6");
        SetBrush("AppPrimaryHoverBrush", isDark ? "#3B82F6" : "#2563EB");
        ApplySystemColors();
    }

    public static SolidColorBrush GetBrush(string key)
    {
        if (ThemeBrushes.TryGetValue(key, out var brush))
        {
            return brush;
        }

        return WpfApplication.Current.Resources[key] as SolidColorBrush
               ?? new SolidColorBrush(Colors.Transparent);
    }

    private static void SetBrush(string key, string colorHex)
    {
        var color = (WpfColor)WpfColorConverter.ConvertFromString(colorHex)!;
        var brush = new SolidColorBrush(color);
        ThemeBrushes[key] = brush;
        WpfApplication.Current.Resources[key] = brush;
    }

    private static void ApplySystemColors()
    {
        var resources = WpfApplication.Current.Resources;
        resources[WpfSystemColors.WindowBrushKey] = GetBrush("AppSurfaceBrush");
        resources[WpfSystemColors.WindowTextBrushKey] = GetBrush("AppTextBrush");
        resources[WpfSystemColors.ControlBrushKey] = GetBrush("AppSurfaceBrush");
        resources[WpfSystemColors.ControlTextBrushKey] = GetBrush("AppTextBrush");
        resources[WpfSystemColors.HighlightBrushKey] = GetBrush("AppPrimaryBrush");
        resources[WpfSystemColors.HighlightTextBrushKey] = WpfBrushes.White;
    }
}
