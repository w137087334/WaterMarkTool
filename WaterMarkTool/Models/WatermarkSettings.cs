using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediaColor = System.Windows.Media.Color;

namespace WaterMarkTool.Models;

public class WatermarkSettings : INotifyPropertyChanged
{
    private string _text = "水印文字";
    private WatermarkPattern _pattern = WatermarkPattern.Tile;
    private int _watermarkCount = 4;
    private WatermarkPosition _position = WatermarkPosition.Center;
    private string _fontFamily = "黑体";
    private bool _isBold;
    private bool _isItalic;
    private MediaColor _color = MediaColor.FromRgb(0, 0, 0);
    private double _opacity = 0.3;
    private double _spacing = 2;
    private double _size = 1;
    private double _rotation = 45;

    private bool _textOutlineEnabled;
    private double _outlineWidth = 2;
    private MediaColor _outlineColor = MediaColor.FromRgb(255, 255, 255);
    private bool _textShadowEnabled;
    private double _shadowOffset = 2;
    private bool _textBackgroundEnabled;
    private double _textBackgroundOpacity = 0.4;

    private bool _useCustomPosition;
    private double _customOffsetX = 0.5;
    private double _customOffsetY = 0.5;

    public WatermarkSettings()
    {
        ImageOverlay = new ImageOverlaySettings();
        ImageOverlay.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.PropertyName))
            {
                OnPropertyChanged(nameof(ImageOverlay));
            }
        };
    }

    public ImageOverlaySettings ImageOverlay { get; }

    public string Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public WatermarkPattern Pattern
    {
        get => _pattern;
        set => SetField(ref _pattern, value);
    }

    public int WatermarkCount
    {
        get => _watermarkCount;
        set => SetField(ref _watermarkCount, Math.Clamp(value, 1, 100));
    }

    public WatermarkPosition Position
    {
        get => _position;
        set => SetField(ref _position, value);
    }

    public string FontFamily
    {
        get => _fontFamily;
        set => SetField(ref _fontFamily, value);
    }

    public bool IsBold
    {
        get => _isBold;
        set => SetField(ref _isBold, value);
    }

    public bool IsItalic
    {
        get => _isItalic;
        set => SetField(ref _isItalic, value);
    }

    public MediaColor Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    public double Opacity
    {
        get => _opacity;
        set => SetField(ref _opacity, Math.Clamp(value, 0, 1));
    }

    public double Spacing
    {
        get => _spacing;
        set => SetField(ref _spacing, Math.Max(0.5, value));
    }

    public double Size
    {
        get => _size;
        set => SetField(ref _size, Math.Clamp(value, 0.1, 5));
    }

    public double Rotation
    {
        get => _rotation;
        set => SetField(ref _rotation, value);
    }

    public bool TextOutlineEnabled
    {
        get => _textOutlineEnabled;
        set => SetField(ref _textOutlineEnabled, value);
    }

    public double OutlineWidth
    {
        get => _outlineWidth;
        set => SetField(ref _outlineWidth, Math.Clamp(value, 0.5, 10));
    }

    public MediaColor OutlineColor
    {
        get => _outlineColor;
        set => SetField(ref _outlineColor, value);
    }

    public bool TextShadowEnabled
    {
        get => _textShadowEnabled;
        set => SetField(ref _textShadowEnabled, value);
    }

    public double ShadowOffset
    {
        get => _shadowOffset;
        set => SetField(ref _shadowOffset, Math.Clamp(value, 1, 20));
    }

    public bool TextBackgroundEnabled
    {
        get => _textBackgroundEnabled;
        set => SetField(ref _textBackgroundEnabled, value);
    }

    public double TextBackgroundOpacity
    {
        get => _textBackgroundOpacity;
        set => SetField(ref _textBackgroundOpacity, Math.Clamp(value, 0, 1));
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

    public void NotifyAllChanged()
    {
        OnPropertyChanged(string.Empty);
    }

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
