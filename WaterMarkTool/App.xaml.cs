using System.Windows;
using WaterMarkTool.Services;

namespace WaterMarkTool;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ThemeManager.Apply(isDark: false);
        base.OnStartup(e);
    }
}
