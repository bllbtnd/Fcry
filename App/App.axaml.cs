using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Fcry.App.Services;
using Fcry.App.ViewModels;
using Fcry.App.Views;
using Fcry.Core.Crypto;
using Fcry.Core.IO;

namespace Fcry.App;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var config = ConfigManager.LoadOrCreate();
            var keyManager = new MasterKeyManager();
            var mainWindow = new MainWindow();
            var picker = new AvaloniaPickerService(mainWindow);
            var mainVm = new MainWindowViewModel(keyManager, config, picker);

            mainWindow.DataContext = mainVm;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
