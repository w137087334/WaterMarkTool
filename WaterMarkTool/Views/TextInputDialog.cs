using System.Windows;

namespace WaterMarkTool.Views;

public class TextInputDialog : Window
{
    public string InputText { get; private set; } = string.Empty;

    public TextInputDialog(string title, string prompt, string defaultValue = "")
    {
        Title = title;
        Width = 420;
        Height = 180;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;

        var grid = new System.Windows.Controls.Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

        var label = new System.Windows.Controls.TextBlock
        {
            Text = prompt,
            Margin = new Thickness(0, 0, 0, 8)
        };
        System.Windows.Controls.Grid.SetRow(label, 0);

        var textBox = new System.Windows.Controls.TextBox
        {
            Text = defaultValue,
            Margin = new Thickness(0, 0, 0, 16)
        };
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
            InputText = textBox.Text.Trim();
            DialogResult = true;
            Close();
        };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        System.Windows.Controls.Grid.SetRow(buttons, 2);

        grid.Children.Add(label);
        grid.Children.Add(textBox);
        grid.Children.Add(buttons);
        Content = grid;
        Loaded += (_, _) => textBox.Focus();
    }

    public static bool TryShow(Window owner, string title, string prompt, out string result, string defaultValue = "")
    {
        var dialog = new TextInputDialog(title, prompt, defaultValue) { Owner = owner };
        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.InputText))
        {
            result = dialog.InputText;
            return true;
        }

        result = string.Empty;
        return false;
    }
}
