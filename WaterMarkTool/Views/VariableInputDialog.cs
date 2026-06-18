using System.Windows;

namespace WaterMarkTool.Views;

public class VariableInputDialog : Window
{
    public string CustomText { get; private set; } = string.Empty;

    public VariableInputDialog()
    {
        Title = "填写变量";
        Width = 440;
        Height = 200;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

        var hint = new System.Windows.Controls.TextBlock
        {
            Text = "水印文字或二维码内容包含 {xx} / XX 等占位符，请填写替换内容：",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 8)
        };
        System.Windows.Controls.Grid.SetRow(hint, 0);

        var textBox = new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 16) };
        System.Windows.Controls.Grid.SetRow(textBox, 1);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var ok = new System.Windows.Controls.Button { Content = "确定", Width = 80, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new System.Windows.Controls.Button { Content = "取消", Width = 80, IsCancel = true };
        ok.Click += (_, _) =>
        {
            CustomText = textBox.Text.Trim();
            DialogResult = true;
            Close();
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        System.Windows.Controls.Grid.SetRow(buttons, 2);

        grid.Children.Add(hint);
        grid.Children.Add(textBox);
        grid.Children.Add(buttons);
        Content = grid;
        Loaded += (_, _) => textBox.Focus();
    }

    public static bool TryShow(Window owner, out string customText)
    {
        var dialog = new VariableInputDialog { Owner = owner };
        if (dialog.ShowDialog() == true)
        {
            customText = dialog.CustomText;
            return true;
        }

        customText = string.Empty;
        return false;
    }
}
