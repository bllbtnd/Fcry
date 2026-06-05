using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fcry.App.Services;
using Fcry.Core.Crypto;
using Fcry.Core.Models;

namespace Fcry.App.ViewModels;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly MasterKeyManager _keyManager;
    private readonly AppConfig _config;
    private readonly IPickerService _picker;

    [ObservableProperty] private ViewModelBase _currentView;

    public MainWindowViewModel(MasterKeyManager keyManager, AppConfig config, IPickerService picker)
    {
        _keyManager = keyManager;
        _config = config;
        _picker = picker;
        _currentView = CreateLockScreen();
    }

    public void ResetInactivity()
    {
        if (CurrentView is MainScreenViewModel vm) vm.ResetInactivityTimer();
    }

    public void RequestLock()
    {
        if (CurrentView is MainScreenViewModel)
            OnLockRequested(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Lock() => RequestLock();

    private LockScreenViewModel CreateLockScreen()
    {
        var vm = new LockScreenViewModel(_keyManager, _config, _picker);
        vm.UnlockSucceeded += OnUnlockSucceeded;
        return vm;
    }

    private MainScreenViewModel CreateMainScreen()
    {
        var vm = new MainScreenViewModel(_keyManager, _picker);
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
