using System.IO;
using System.Text.RegularExpressions;
using WaterMarkTool.Models;

namespace WaterMarkTool.Services;

public static class WatermarkTextResolver
{
  private static readonly Regex PlaceholderRegex = new(@"\{(date|time|datetime|filename|index|total|frame|frameTotal|xx)\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  public static bool ContainsPlaceholders(string? text)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return false;
    }

    return PlaceholderRegex.IsMatch(text) || text.Contains("XX", StringComparison.Ordinal);
  }

  public static string Resolve(string? text, WatermarkRenderContext context)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      return string.Empty;
    }

    var result = text.Trim();
    var now = DateTime.Now;
    var fileName = Path.GetFileNameWithoutExtension(context.FileName);

    result = PlaceholderRegex.Replace(result, match =>
    {
      return match.Groups[1].Value.ToLowerInvariant() switch
      {
        "date" => now.ToString("yyyy-MM-dd"),
        "time" => now.ToString("HH:mm:ss"),
        "datetime" => now.ToString("yyyy-MM-dd HH:mm"),
        "filename" => string.IsNullOrEmpty(fileName) ? "图片" : fileName,
        "index" => context.Index.ToString(),
        "total" => context.Total.ToString(),
        "frame" => context.FrameIndex.ToString(),
        "frametotal" => context.FrameTotal.ToString(),
        "xx" => string.IsNullOrEmpty(context.CustomText) ? "XX" : context.CustomText,
        _ => match.Value
      };
    });

    if (!string.IsNullOrEmpty(context.CustomText))
    {
      result = result.Replace("XX", context.CustomText, StringComparison.Ordinal);
    }

    return result;
  }
}
