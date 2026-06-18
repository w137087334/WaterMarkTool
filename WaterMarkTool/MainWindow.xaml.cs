using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WaterMarkTool.Models;
using WaterMarkTool.Services;
using WaterMarkTool.ViewModels;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;

namespace WaterMarkTool;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;
    private bool _isDraggingPosition;
    private Point _dragStart;

    public MainWindow()
    {
        InitializeComponent();
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.Preview.PropertyChanged -= Preview_PropertyChanged;
        }

        _viewModel = vm;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        _viewModel.Preview.PropertyChanged += Preview_PropertyChanged;
        ApplyTheme(vm.IsDarkTheme);
        UpdatePositionEditorSize();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _viewModel?.SaveSessionSettings();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDarkTheme) && sender is MainViewModel vm)
        {
            ApplyTheme(vm.IsDarkTheme);
        }

        if (e.PropertyName is nameof(MainViewModel.IsPreviewOpen)
            or nameof(MainViewModel.PreviewImage)
            or nameof(MainViewModel.SourcePreviewImage)
            or nameof(MainViewModel.PreviewMode))
        {
            UpdatePositionEditorSize();
        }
    }

    private void Preview_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PreviewViewModel.IsPreviewOpen) || e.PropertyName == nameof(PreviewViewModel.PreviewIndex))
        {
            UpdatePositionEditorSize();
        }
    }

    private void ApplyTheme(bool isDark)
    {
        ThemeManager.Apply(isDark);
        Background = ThemeManager.GetBrush("AppBackgroundBrush");
        DropZone.BorderBrush = ThemeManager.GetBrush("AppPrimaryBrush");
        DropZone.Background = new SolidColorBrush(isDark
            ? WpfColor.FromArgb(0x33, 0x60, 0xA5, 0xFA)
            : WpfColor.FromArgb(0x14, 0x3B, 0x82, 0xF6));
    }

    private async void DropZone_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            await vm.AddImagesCommand.ExecuteAsync(null);
        }
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
        DropZone.BorderBrush = ThemeManager.GetBrush("AppPrimaryBrush");
        if (DataContext is MainViewModel { IsDarkTheme: true })
        {
            DropZone.Background = new SolidColorBrush(WpfColor.FromArgb(0x33, 0x60, 0xA5, 0xFA));
        }
        else
        {
            DropZone.Background = new SolidColorBrush(WpfColor.FromArgb(0x33, 0x3B, 0x82, 0xF6));
        }
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        DropZone.BorderBrush = ThemeManager.GetBrush("AppPrimaryBrush");
        if (DataContext is MainViewModel { IsDarkTheme: true })
        {
            DropZone.Background = new SolidColorBrush(WpfColor.FromArgb(0x14, 0x60, 0xA5, 0xFA));
        }
        else
        {
            DropZone.Background = new SolidColorBrush(WpfColor.FromArgb(0x14, 0x3B, 0x82, 0xF6));
        }
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        DropZone_DragLeave(sender, e);
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || DataContext is not MainViewModel vm)
        {
            return;
        }

        await vm.LoadImagesAsync(files);
    }

    private void ColorPreview_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        var picker = new ColorPickerWindow(vm.Settings.Color)
        {
            Owner = this
        };

        if (picker.ShowDialog() == true)
        {
            vm.Settings.Color = picker.SelectedColor;
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        if (vm.IsPreviewOpen)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    if (vm.IsPositionEditorOpen)
                    {
                        vm.TogglePositionEditorCommand.Execute(null);
                    }
                    else
                    {
                        vm.ClosePreviewCommand.Execute(null);
                    }

                    e.Handled = true;
                    break;
                case Key.Left:
                    vm.PreviewPreviousCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Right:
                    vm.PreviewNextCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.Tab:
                    vm.CyclePreviewModeCommand.Execute(null);
                    e.Handled = true;
                    break;
                case Key.C when !IsTextInputFocused():
                    vm.TogglePositionEditorCommand.Execute(null);
                    e.Handled = true;
                    break;
            }

            return;
        }

        if (IsTextInputFocused())
        {
            return;
        }

        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            vm.UndoDeleteCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            _ = vm.PasteImageAsync();
        }
    }

    private static bool IsTextInputFocused()
    {
        return Keyboard.FocusedElement is TextBox
               or System.Windows.Controls.Primitives.TextBoxBase;
    }

    private void Thumbnail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement element
            && element.DataContext is WatermarkImageItem item
            && DataContext is MainViewModel vm)
        {
            vm.OpenPreviewCommand.Execute(item);
            e.Handled = true;
        }
    }

    private void PreviewOverlay_BackgroundClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource == sender && DataContext is MainViewModel vm)
        {
            vm.ClosePreviewCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void PreviewImage_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void UpdatePositionEditorSize()
    {
        var reference = PreviewWatermarkedImage.Visibility == Visibility.Visible
            ? PreviewWatermarkedImage
            : PreviewOriginalImage.Visibility == Visibility.Visible
                ? PreviewOriginalImage
                : PreviewWatermarkedImage;

        if (reference.Source == null)
        {
            PositionEditorCanvas.Width = 0;
            PositionEditorCanvas.Height = 0;
            return;
        }

        reference.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = reference.DesiredSize.Width;
        var height = reference.DesiredSize.Height;
        if (width <= 0 || height <= 0)
        {
            width = reference.MaxWidth;
            height = reference.MaxHeight;
        }

        PositionEditorCanvas.Width = width;
        PositionEditorCanvas.Height = height;
        DrawPositionMarker();
    }

    private void DrawPositionMarker()
    {
        PositionEditorCanvas.Children.Clear();
        if (DataContext is not MainViewModel vm || PositionEditorCanvas.Width <= 0)
        {
            return;
        }

        var isOverlay = vm.Settings.ImageOverlay.Mode != ImageOverlayMode.None;
        var x = isOverlay ? vm.Settings.ImageOverlay.CustomOffsetX : vm.Settings.CustomOffsetX;
        var y = isOverlay ? vm.Settings.ImageOverlay.CustomOffsetY : vm.Settings.CustomOffsetY;

        var marker = new Border
        {
            Width = 24,
            Height = 24,
            BorderBrush = Brushes.Yellow,
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(WpfColor.FromArgb(80, 255, 255, 0)),
            Cursor = Cursors.Hand
        };
        Canvas.SetLeft(marker, x * PositionEditorCanvas.Width - 12);
        Canvas.SetTop(marker, y * PositionEditorCanvas.Height - 12);
        PositionEditorCanvas.Children.Add(marker);
    }

    private void PositionEditor_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingPosition = true;
        _dragStart = e.GetPosition(PositionEditorCanvas);
        PositionEditorCanvas.CaptureMouse();
        UpdatePositionFromPoint(_dragStart);
        e.Handled = true;
    }

    private void PositionEditor_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingPosition)
        {
            return;
        }

        UpdatePositionFromPoint(e.GetPosition(PositionEditorCanvas));
        e.Handled = true;
    }

    private void PositionEditor_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingPosition = false;
        PositionEditorCanvas.ReleaseMouseCapture();
        e.Handled = true;
    }

    private void UpdatePositionFromPoint(Point point)
    {
        if (DataContext is not MainViewModel vm || PositionEditorCanvas.Width <= 0 || PositionEditorCanvas.Height <= 0)
        {
            return;
        }

        var x = Math.Clamp(point.X / PositionEditorCanvas.Width, 0, 1);
        var y = Math.Clamp(point.Y / PositionEditorCanvas.Height, 0, 1);
        var isOverlay = vm.Settings.ImageOverlay.Mode != ImageOverlayMode.None;
        vm.ApplyDraggedPosition(x, y, isOverlay);
        DrawPositionMarker();
    }
}
