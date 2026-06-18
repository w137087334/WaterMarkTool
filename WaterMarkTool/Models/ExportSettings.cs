using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WaterMarkTool.Models;

public enum ExportFormat
{
    KeepOriginal,
    Png,
    Jpeg
}

public class ExportSettings : INotifyPropertyChanged
{
    private ExportFormat _format = ExportFormat.KeepOriginal;
    private int _jpegQuality = 90;
    private bool _preserveMetadata = true;

    public ExportFormat Format
    {
        get => _format;
        set => SetField(ref _format, value);
    }

    public int JpegQuality
    {
        get => _jpegQuality;
        set => SetField(ref _jpegQuality, Math.Clamp(value, 1, 100));
    }

    public bool PreserveMetadata
    {
        get => _preserveMetadata;
        set => SetField(ref _preserveMetadata, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
