using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WaterMarkTool.Models;

namespace WaterMarkTool.ViewModels;

public partial class PreviewViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isPreviewOpen;

    [ObservableProperty]
    private int _previewIndex = -1;

    [ObservableProperty]
    private string _previewFileName = string.Empty;

    [ObservableProperty]
    private PreviewMode _previewMode = PreviewMode.Watermarked;

    [ObservableProperty]
    private bool _isPositionEditorOpen;

    [ObservableProperty]
    private double _sliderPosition = 0.5;

    partial void OnPreviewModeChanged(PreviewMode value)
    {
        OnPropertyChanged(nameof(IsCompareMode));
        OnPropertyChanged(nameof(IsSliderMode));
    }

    public bool IsCompareMode => PreviewMode is PreviewMode.SideBySide or PreviewMode.Slider;
    public bool IsSliderMode => PreviewMode == PreviewMode.Slider;

    [RelayCommand]
    public void CyclePreviewMode()
    {
        PreviewMode = PreviewMode switch
        {
            PreviewMode.Watermarked => PreviewMode.Original,
            PreviewMode.Original => PreviewMode.SideBySide,
            PreviewMode.SideBySide => PreviewMode.Slider,
            _ => PreviewMode.Watermarked
        };
    }

    public void Reset()
    {
        IsPreviewOpen = false;
        PreviewIndex = -1;
        PreviewFileName = string.Empty;
        IsPositionEditorOpen = false;
        PreviewMode = PreviewMode.Watermarked;
        SliderPosition = 0.5;
    }
}
