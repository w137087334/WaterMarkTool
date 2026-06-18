using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WaterMarkTool.Controls;

public class CompareSlider : UserControl
{
    public static readonly DependencyProperty LeftImageProperty =
        DependencyProperty.Register(
            nameof(LeftImage),
            typeof(ImageSource),
            typeof(CompareSlider),
            new PropertyMetadata(null, OnImageChanged));

    public static readonly DependencyProperty RightImageProperty =
        DependencyProperty.Register(
            nameof(RightImage),
            typeof(ImageSource),
            typeof(CompareSlider),
            new PropertyMetadata(null, OnImageChanged));

    public static readonly DependencyProperty SliderPositionProperty =
        DependencyProperty.Register(
            nameof(SliderPosition),
            typeof(double),
            typeof(CompareSlider),
            new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSliderChanged));

    private readonly Canvas _canvas;
    private bool _isDragging;
    private bool _isBuilt;
    private double _lastWidth;
    private double _lastHeight;

    public ImageSource? LeftImage
    {
        get => (ImageSource?)GetValue(LeftImageProperty);
        set => SetValue(LeftImageProperty, value);
    }

    public ImageSource? RightImage
    {
        get => (ImageSource?)GetValue(RightImageProperty);
        set => SetValue(RightImageProperty, value);
    }

    public double SliderPosition
    {
        get => (double)GetValue(SliderPositionProperty);
        set => SetValue(SliderPositionProperty, value);
    }

    public CompareSlider()
    {
        _canvas = new Canvas { ClipToBounds = true };
        Content = _canvas;
        MinWidth = 320;
        MinHeight = 240;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        Loaded += (_, _) => RebuildIfReady();
        IsVisibleChanged += (_, _) => RebuildIfReady();
        SizeChanged += (_, _) =>
        {
            if (Math.Abs(ActualWidth - _lastWidth) > 1 || Math.Abs(ActualHeight - _lastHeight) > 1)
            {
                _isBuilt = false;
                _lastWidth = ActualWidth;
                _lastHeight = ActualHeight;
                RebuildIfReady();
            }
        };
    }

    private static void OnImageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CompareSlider slider)
        {
            slider._isBuilt = false;
            slider.RebuildIfReady();
        }
    }

    private static void OnSliderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CompareSlider slider)
        {
            slider.UpdateClip();
        }
    }

    private void RebuildIfReady()
    {
        if (!IsVisible || LeftImage == null || RightImage == null)
        {
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : RenderSize.Width;
        var height = ActualHeight > 0 ? ActualHeight : RenderSize.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (_isBuilt && _canvas.Children.Count >= 3)
        {
            UpdateClip();
            return;
        }

        _canvas.Width = width;
        _canvas.Height = height;
        _canvas.Children.Clear();

        var right = new Image
        {
            Source = RightImage,
            Stretch = Stretch.Uniform,
            Width = width,
            Height = height
        };
        _canvas.Children.Add(right);

        var leftContainer = new Canvas
        {
            Width = width,
            Height = height,
            ClipToBounds = true
        };
        var left = new Image
        {
            Source = LeftImage,
            Stretch = Stretch.Uniform,
            Width = width,
            Height = height
        };
        leftContainer.Children.Add(left);
        _canvas.Children.Add(leftContainer);

        var handle = new Border
        {
            Width = 4,
            Height = height,
            Background = Brushes.White,
            Cursor = Cursors.SizeWE
        };
        handle.MouseLeftButtonDown += (_, e) =>
        {
            _isDragging = true;
            handle.CaptureMouse();
            e.Handled = true;
        };
        handle.MouseMove += (_, e) =>
        {
            if (!_isDragging)
            {
                return;
            }

            var pos = e.GetPosition(_canvas);
            SliderPosition = Math.Clamp(pos.X / width, 0.02, 0.98);
            e.Handled = true;
        };
        handle.MouseLeftButtonUp += (_, _) =>
        {
            _isDragging = false;
            handle.ReleaseMouseCapture();
        };
        _canvas.Children.Add(handle);

        _isBuilt = true;
        UpdateClip();
    }

    private void UpdateClip()
    {
        if (!_isBuilt || _canvas.Children.Count < 3)
        {
            return;
        }

        var width = _canvas.Width;
        var height = _canvas.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var leftContainer = (Canvas)_canvas.Children[1];
        var handle = (Border)_canvas.Children[2];
        var x = width * SliderPosition;
        leftContainer.Width = x;
        leftContainer.Clip = new RectangleGeometry(new Rect(0, 0, x, height));
        Canvas.SetLeft(handle, x - handle.Width / 2);
    }
}
