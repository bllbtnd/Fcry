using CommunityToolkit.Mvvm.ComponentModel;
using Fcry.Core.Crypto;
using Fcry.Core.Models;

namespace Fcry.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly MasterKeyManager _keyManager;
    private readonly AppConfig _config;
    private readonly Func<Task<string?>> _pickKeyFile;

    [ObservableProperty] private ViewModelBase _currentView;

    public MainWindowViewModel(MasterKeyManager keyManager, AppConfig config, Func<Task<string?>> pickKeyFile)
    {
        _keyManager = keyManager;
        _config = config;
        _pickKeyFile = pickKeyFile;
        _currentView = CreateLockScreen();
    }

    public void ResetInactivity()
    {
        if (CurrentView is MainScreenViewModel mainVm)
            mainVm.ResetInactivityTimer();
    }

    private LockScreenViewModel CreateLockScreen()
    {
        var vm = new LockScreenViewModel(_keyManager, _config, _pickKeyFile);
        vm.UnlockSucceeded += OnUnlockSucceeded;
        return vm;
    }

    private MainScreenViewModel CreateMainScreen()
    {
        var vm = new MainScreenViewModel(_keyManager);
        vm.LockRequested += OnLockRequested;
        return vm;
    }

    private void OnUnlockSucceeded(object? sender, EventArgs e)
    {
        if (CurrentView is LockScreenViewModel old)
            old.UnlockSucceeded -= OnUnlockSucceeded;
        CurrentView = CreateMainScreen();
    }

    private void OnLockRequested(object? sender, EventArgs e)
    {
        if (CurrentView is MainScreenViewModel mainVm)
        {
            mainVm.LockRequested -= OnLockRequested;
            mainVm.Dispose();
        }
        _keyManager.Lock();
        CurrentView = CreateLockScreen();
    }
}
