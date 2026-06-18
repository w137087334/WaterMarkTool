using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WaterMarkTool.Models;

public enum ImageOverlayMode
{
    None,
    Logo,
    QrCode
}

public class ImageOverlaySettings : INotifyPropertyChanged
{
    private ImageOverlayMode _mode = ImageOverlayMode.None;
    private string _logoPath = string.Empty;
    private string _qrContent = string.Empty;
    private double _opacity = 0.8;
    private double _sizeScale = 1.0;
    private WatermarkPosition _position = WatermarkPosition.BottomRight;
    private double _rotation;
    private double _marginPercent = 0.1;
    private bool _useCustomPosition;
    private double _customOffsetX = 0.85;
    private double _customOffsetY = 0.9;

    public ImageOverlayMode Mode
    {
        get => _mode;
        set => SetField(ref _mode, value);
    }

    public string LogoPath
    {
        get => _logoPath;
        set => SetField(ref _logoPath, value);
    }

    public string QrContent
    {
        get => _qrContent;
        set => SetField(ref _qrContent, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, Math.Clamp(value, 0, 1));
    }

    public double SizeScale
    {
        get => _sizeScale;
        set => SetField(ref _sizeScale, Math.Clamp(value, 0.1, 5));
    }

    public WatermarkPosition Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }

    public double Rotation
    {
        get => _rotation;
        set => SetField(ref _rotation, value);
    }

    public double MarginPercent
    {
        get => _marginPercent;
        set => SetField(ref _marginPercent, Math.Clamp(value, 0, 0.5));
    }

    public bool UseCustomPosition
    {
        get => _useCustomPosition;
        set => SetField(ref _useCustomPosition, value);
    }

    public double CustomOffsetX
    {
        get => _customOffsetX;
        set => SetField(ref _customOffsetX, Math.Clamp(value, 0, 1));
    }

    public double CustomOffsetY
    {
        get => _customOffsetY;
        set => SetField(ref _customOffsetY, Math.Clamp(value, 0, 1));
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
