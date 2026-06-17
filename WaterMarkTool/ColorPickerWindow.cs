using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using WaterMarkTool.Services;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;
using WpfButton = System.Windows.Controls.Button;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace WaterMarkTool;

public partial class ColorPickerWindow : Window
{
    private readonly Border _preview;
    private readonly WpfTextBox _hexBox;
    private readonly Slider _redSlider;
    private readonly Slider _greenSlider;
    private readonly Slider _blueSlider;
    private readonly TextBlock _presetTitle;
    private readonly Border _buttonBar;
    private bool _updating;

    public MediaColor SelectedColor { get; private set; }

    public ColorPickerWindow(MediaColor initialColor)
    {
        SelectedColor = initialColor;
        Title = "选择颜色";
        Width = 380;
        MinHeight = 480;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        Background = ThemeManager.GetBrush("AppBackgroundBrush");

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var root = new StackPanel { Margin = new Thickness(20, 20, 20, 12) };

        _preview = new Border
        {
            Height = 52,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(initialColor),
            BorderBrush = ThemeManager.GetBrush("AppControlBorderBrush"),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 16)
        };
        root.Children.Add(_preview);

        _hexBox = new WpfTextBox
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        _hexBox.LostFocus += (_, _) => ApplyHexInput();
        root.Children.Add(_hexBox);

        _redSlider = CreateChannelSlider("R", initialColor.R, out var redPanel);
        _greenSlider = CreateChannelSlider("G", initialColor.G, out var greenPanel);
        _blueSlider = CreateChannelSlider("B", initialColor.B, out var bluePanel);
        root.Children.Add(redPanel);
        root.Children.Add(greenPanel);
        root.Children.Add(bluePanel);

        _redSlider.ValueChanged += (_, _) => OnChannelChanged();
        _greenSlider.ValueChanged += (_, _) => OnChannelChanged();
        _blueSlider.ValueChanged += (_, _) => OnChannelChanged();

        _presetTitle = new TextBlock
        {
            Text = "常用颜色",
            Margin = new Thickness(0, 4, 0, 8),
            Foreground = ThemeManager.GetBrush("AppSecondaryTextBrush")
        };
        root.Children.Add(_presetTitle);

        var presets = new WrapPanel();
        foreach (var color in GetPresetColors())
        {
            var button = new WpfButton
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(0, 0, 8, 8),
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(1),
                BorderBrush = ThemeManager.GetBrush("AppControlBorderBrush"),
                Tag = color,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            button.Click += (_, _) => SetColor(color);
            presets.Children.Add(button);
        }

        root.Children.Add(presets);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = root
        };
        Grid.SetRow(scroll, 0);
        layout.Children.Add(scroll);

        _buttonBar = new Border
        {
            Padding = new Thickness(20, 0, 20, 16),
            Background = ThemeManager.GetBrush("AppBackgroundBrush"),
            BorderBrush = ThemeManager.GetBrush("AppBorderBrush"),
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
        var buttons = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = WpfHorizontalAlignment.Right
        };

        var okButton = new WpfButton
        {
            Content = "确定",
            Width = 88,
            Margin = new Thickness(0, 12, 8, 0),
            Style = (Style)WpfApplication.Current.FindResource("PrimaryButton")
        };
        var cancelButton = new WpfButton
        {
            Content = "取消",
            Width = 88,
            Margin = new Thickness(0, 12, 0, 0),
            Style = (Style)WpfApplication.Current.FindResource("SecondaryButton")
        };

        okButton.Click += (_, _) =>
        {
            SelectedColor = GetCurrentColor();
            DialogResult = true;
        };
        cancelButton.Click += (_, _) => DialogResult = false;
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);
        _buttonBar.Child = buttons;
        Grid.SetRow(_buttonBar, 1);
        layout.Children.Add(_buttonBar);

        Content = layout;
        SetColor(initialColor);
    }

    private Slider CreateChannelSlider(string label, byte value, out Grid panel)
    {
        panel = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });

        var name = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = ThemeManager.GetBrush("AppSecondaryTextBrush")
        };
        Grid.SetColumn(name, 0);
        panel.Children.Add(name);

        var slider = new Slider
        {
            Minimum = 0,
            Maximum = 255,
            Value = value,
            TickFrequency = 1,
            IsSnapToTickEnabled = true,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 8, 0)
        };
        Grid.SetColumn(slider, 1);
        panel.Children.Add(slider);

        var valueText = new TextBlock
        {
            Text = value.ToString(CultureInfo.InvariantCulture),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = WpfHorizontalAlignment.Right,
            Foreground = ThemeManager.GetBrush("AppTextBrush")
        };
        Grid.SetColumn(valueText, 2);
        panel.Children.Add(valueText);

        slider.ValueChanged += (_, _) =>
        {
            valueText.Text = ((int)slider.Value).ToString(CultureInfo.InvariantCulture);
        };

        slider.Foreground = label switch
        {
            "R" => new SolidColorBrush(MediaColor.FromRgb(239, 68, 68)),
            "G" => new SolidColorBrush(MediaColor.FromRgb(34, 197, 94)),
            _ => new SolidColorBrush(MediaColor.FromRgb(59, 130, 246))
        };

        return slider;
    }

    private void OnChannelChanged()
    {
        if (_updating)
        {
            return;
        }

        SetColor(GetCurrentColor());
    }

    private MediaColor GetCurrentColor()
    {
        return MediaColor.FromRgb(
            (byte)_redSlider.Value,
            (byte)_greenSlider.Value,
            (byte)_blueSlider.Value);
    }

    private void SetColor(MediaColor color)
    {
        _updating = true;
        SelectedColor = color;
        _preview.Background = new SolidColorBrush(color);
        _hexBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        _redSlider.Value = color.R;
        _greenSlider.Value = color.G;
        _blueSlider.Value = color.B;
        _updating = false;
    }

    private void ApplyHexInput()
    {
        try
        {
            var text = _hexBox.Text.Trim();
            if (!text.StartsWith('#'))
            {
                text = "#" + text;
            }

            var color = (MediaColor)WpfColorConverter.ConvertFromString(text)!;
            SetColor(color);
        }
        catch
        {
            MessageBox.Show("颜色格式无效，请输入如 #000000", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            _hexBox.Text = $"#{SelectedColor.R:X2}{SelectedColor.G:X2}{SelectedColor.B:X2}";
        }
    }

    private static IEnumerable<MediaColor> GetPresetColors()
    {
        return
        [
            MediaColor.FromRgb(0, 0, 0),
            MediaColor.FromRgb(30, 41, 59),
            MediaColor.FromRgb(100, 116, 139),
            MediaColor.FromRgb(255, 255, 255),
            MediaColor.FromRgb(239, 68, 68),
            MediaColor.FromRgb(249, 115, 22),
            MediaColor.FromRgb(245, 158, 11),
            MediaColor.FromRgb(34, 197, 94),
            MediaColor.FromRgb(16, 185, 129),
            MediaColor.FromRgb(59, 130, 246),
            MediaColor.FromRgb(99, 102, 241),
            MediaColor.FromRgb(168, 85, 247)
        ];
    }
}
