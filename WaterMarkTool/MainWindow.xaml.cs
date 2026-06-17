using System.ComponentModel;
using System.Windows;
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
        }

        _viewModel = vm;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        ApplyTheme(vm.IsDarkTheme);
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsDarkTheme) && sender is MainViewModel vm)
        {
            ApplyTheme(vm.IsDarkTheme);
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

    private void DropZone_DragOver(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
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

    private void DropZone_DragLeave(object sender, System.Windows.DragEventArgs e)
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

    private async void DropZone_Drop(object sender, System.Windows.DragEventArgs e)
    {
        DropZone_DragLeave(sender, e);
        if (e.Data.GetData(System.Windows.DataFormats.FileDrop) is not string[] files || DataContext is not MainViewModel vm)
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

    private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
                    vm.ClosePreviewCommand.Execute(null);
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
        return Keyboard.FocusedElement is System.Windows.Controls.TextBox
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
        if (DataContext is MainViewModel vm)
        {
            vm.ClosePreviewCommand.Execute(null);
        }
    }
}
