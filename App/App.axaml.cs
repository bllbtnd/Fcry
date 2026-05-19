using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
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

            async Task<string?> PickKeyFile()
            {
                var window = desktop.MainWindow;
                if (window == null) return null;
                var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "Select Key File",
                    AllowMultiple = false
                });
                return files.Count > 0 ? files[0].Path.LocalPath : null;
            }

            var mainVm = new MainWindowViewModel(keyManager, config, PickKeyFile);
            desktop.MainWindow = new MainWindow { DataContext = mainVm };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
