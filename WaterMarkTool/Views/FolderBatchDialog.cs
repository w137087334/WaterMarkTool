using System.IO;
using System.Windows;
using Microsoft.Win32;
using WaterMarkTool.Models;
using WaterMarkTool.Services;

namespace WaterMarkTool.Views;

public partial class FolderBatchDialog : Window
{
    public FolderBatchOptions? Result { get; private set; }

    public FolderBatchDialog()
    {
        Title = "文件夹批处理";
        Width = 520;
        Height = 320;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        BuildUi();
    }

    private System.Windows.Controls.TextBox? _inputBox;
    private System.Windows.Controls.TextBox? _outputBox;
    private System.Windows.Controls.CheckBox? _subfolderCheck;

    private void BuildUi()
    {
        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        for (var i = 0; i < 6; i++)
        {
            grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        }

        AddFolderRow(grid, 0, "输入文件夹", out _inputBox, PickInput);
        AddFolderRow(grid, 1, "输出文件夹", out _outputBox, PickOutput);

        _subfolderCheck = new System.Windows.Controls.CheckBox
        {
            Content = "包含子文件夹",
            Margin = new Thickness(0, 12, 0, 12),
            IsChecked = true
        };
        System.Windows.Controls.Grid.SetRow(_subfolderCheck, 2);
        grid.Children.Add(_subfolderCheck);

        var hint = new System.Windows.Controls.TextBlock
        {
            Text = "将使用当前水印与导出设置，直接输出到目标文件夹（不加载到列表）。",
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.Gray,
            Margin = new Thickness(0, 0, 0, 16)
        };
        System.Windows.Controls.Grid.SetRow(hint, 3);
        grid.Children.Add(hint);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var start = new System.Windows.Controls.Button { Content = "开始处理", Width = 100, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new System.Windows.Controls.Button { Content = "取消", Width = 80, IsCancel = true };
        start.Click += (_, _) => OnStart();
        buttons.Children.Add(start);
        buttons.Children.Add(cancel);
        System.Windows.Controls.Grid.SetRow(buttons, 4);
        grid.Children.Add(buttons);

        Content = grid;
    }

    private static void AddFolderRow(
        System.Windows.Controls.Grid grid,
        int row,
        string label,
        out System.Windows.Controls.TextBox textBox,
        RoutedEventHandler browse)
    {
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
        var rowPanel = new System.Windows.Controls.Grid();
        rowPanel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition());
        rowPanel.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
        textBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 8, 0) };
        System.Windows.Controls.Grid.SetColumn(textBox, 0);
        var browseBtn = new System.Windows.Controls.Button { Content = "浏览...", Width = 72 };
        browseBtn.Click += browse;
        System.Windows.Controls.Grid.SetColumn(browseBtn, 1);
        rowPanel.Children.Add(textBox);
        rowPanel.Children.Add(browseBtn);
        panel.Children.Add(rowPanel);
        System.Windows.Controls.Grid.SetRow(panel, row);
        grid.Children.Add(panel);
    }

    private void PickInput(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            _inputBox!.Text = dialog.FolderName;
            if (string.IsNullOrWhiteSpace(_outputBox!.Text))
            {
                _outputBox.Text = Path.Combine(dialog.FolderName, "watermarked");
            }
        }
    }

    private void PickOutput(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog() == true)
        {
            _outputBox!.Text = dialog.FolderName;
        }
    }

    private void OnStart()
    {
        if (string.IsNullOrWhiteSpace(_inputBox?.Text) || !Directory.Exists(_inputBox.Text))
        {
            MessageBox.Show("请选择有效的输入文件夹", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var output = string.IsNullOrWhiteSpace(_outputBox?.Text)
            ? Path.Combine(_inputBox.Text, "watermarked")
            : _outputBox.Text;

        Result = new FolderBatchOptions
        {
            InputFolder = _inputBox.Text,
            OutputFolder = output,
            IncludeSubfolders = _subfolderCheck?.IsChecked == true
        };
        DialogResult = true;
        Close();
    }

    public static async Task<FolderBatchResult?> RunAsync(
        Window owner,
        WatermarkSettings settings,
        ExportSettings exportSettings,
        string customText)
    {
        var dialog = new FolderBatchDialog { Owner = owner };
        if (dialog.ShowDialog() != true || dialog.Result == null)
        {
            return null;
        }

        var processor = new FolderBatchProcessor();
        var progress = new Progress<FolderBatchProgress>();
        return await processor.ProcessAsync(dialog.Result, settings, exportSettings, customText, progress);
    }
}
