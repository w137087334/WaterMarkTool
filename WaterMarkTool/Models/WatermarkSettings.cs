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
