using System.IO;
using System.Text.Json;
using WaterMarkTool.Models;

namespace WaterMarkTool.Services;

public sealed class PresetStorageService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _directory;
    private readonly string _lastSettingsPath;
    private readonly string _presetsPath;

    public PresetStorageService()
    {
        _directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WaterMarkTool");
        _lastSettingsPath = Path.Combine(_directory, "last-settings.json");
        _presetsPath = Path.Combine(_directory, "presets.json");
    }

    public void SaveLastSettings(WatermarkSettings settings, ExportSettings exportSettings)
    {
        Directory.CreateDirectory(_directory);
        var payload = new SessionSettingsDto
        {
            Watermark = WatermarkSettingsMapper.ToDto(settings),
            Export = new ExportSettingsDto
            {
                Format = exportSettings.Format,
                JpegQuality = exportSettings.JpegQuality,
                PreserveMetadata = exportSettings.PreserveMetadata
            }
        };
        File.WriteAllText(_lastSettingsPath, JsonSerializer.Serialize(payload, JsonOptions));
    }

    public SessionSettingsDto? LoadLastSettings()
    {
        if (!File.Exists(_lastSettingsPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<SessionSettingsDto>(File.ReadAllText(_lastSettingsPath));
        }
        catch
        {
            return null;
        }
    }

    public IReadOnlyList<WatermarkPreset> LoadPresets()
    {
        if (!File.Exists(_presetsPath))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<WatermarkPreset>>(File.ReadAllText(_presetsPath)) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void SavePresets(IEnumerable<WatermarkPreset> presets)
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_presetsPath, JsonSerializer.Serialize(presets.ToList(), JsonOptions));
    }
}

public sealed class SessionSettingsDto
{
    public WatermarkSettingsDto Watermark { get; set; } = new();
    public ExportSettingsDto Export { get; set; } = new();
}

public sealed class ExportSettingsDto
{
    public ExportFormat Format { get; set; } = ExportFormat.KeepOriginal;
    public int JpegQuality { get; set; } = 90;
    public bool PreserveMetadata { get; set; } = true;
}
