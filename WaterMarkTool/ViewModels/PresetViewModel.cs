using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterMarkTool.Models;
using WaterMarkTool.Services;
using WaterMarkTool.Views;

namespace WaterMarkTool.ViewModels;

public partial class PresetViewModel : ObservableObject
{
    private readonly PresetStorageService _storage = new();

    public ObservableCollection<WatermarkPreset> UserPresets { get; } = [];

    [ObservableProperty]
    private WatermarkPreset? _selectedPreset;

    public PresetViewModel()
    {
        Reload();
    }

    public void Reload()
    {
        UserPresets.Clear();
        foreach (var preset in _storage.LoadPresets())
        {
            UserPresets.Add(preset);
        }
    }

    public bool TrySavePreset(System.Windows.Window owner, WatermarkSettings settings)
    {
        if (!TextInputDialog.TryShow(owner, "保存预设", "请输入预设名称：", out var name))
        {
            return false;
        }

        var preset = new WatermarkPreset
        {
            Name = name,
            Settings = WatermarkSettingsMapper.ToDto(settings)
        };

        var existing = UserPresets.FirstOrDefault(p => p.Name == name);
        if (existing != null)
        {
            UserPresets[UserPresets.IndexOf(existing)] = preset;
        }
        else
        {
            UserPresets.Add(preset);
        }

        _storage.SavePresets(UserPresets);
        SelectedPreset = preset;
        return true;
    }

    public void ApplyPreset(WatermarkSettings settings, WatermarkPreset? preset)
    {
        if (preset == null)
        {
            return;
        }

        WatermarkSettingsMapper.ApplyDto(settings, preset.Settings);
    }

    public void DeleteSelectedPreset()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        UserPresets.Remove(SelectedPreset);
        _storage.SavePresets(UserPresets);
        SelectedPreset = null;
    }
}
