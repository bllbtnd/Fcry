using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fcry.App.Services;
using Fcry.Core.Crypto;
using Fcry.Core.Models;

namespace Fcry.App.ViewModels;

public sealed partial class LockScreenViewModel : ViewModelBase
{
    private readonly MasterKeyManager _keyManager;
    private readonly AppConfig _config;
    private readonly IPickerService _picker;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UnlockCommand))]
    [NotifyPropertyChangedFor(nameof(UnlockButtonText))]
    private bool _isUnlocking;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UnlockCommand))]
    private string _passphrase = string.Empty;

    [ObservableProperty] private string? _keyFilePath;
    [ObservableProperty] private string? _errorMessage;

    public string UnlockButtonText => IsUnlocking ? "Unlocking..." : "Unlock";

    public event EventHandler? UnlockSucceeded;

    public LockScreenViewModel(MasterKeyManager keyManager, AppConfig config, IPickerService picker)
    {
        _keyManager = keyManager;
        _config = config;
        _picker = picker;
    }

    [RelayCommand(CanExecute = nameof(CanUnlock))]
    private async Task UnlockAsync()
    {
        IsUnlocking = true;
        ErrorMessage = null;

        var passphraseBytes = Encoding.UTF8.GetBytes(Passphrase);
        Passphrase = string.Empty;

        try
        {
            var key = await Task.Run(() => ArgonKeyDerivation.DeriveKey(passphraseBytes, _config.ArgonSalt));

            try
            {
                if (!string.IsNullOrEmpty(KeyFilePath))
                {
                    var keyFileData = await File.ReadAllBytesAsync(KeyFilePath);
                    var keyFileHash = SHA256.HashData(keyFileData);
                    for (var i = 0; i < 32; i++)
                        key[i] ^= keyFileHash[i];
                    CryptographicOperations.ZeroMemory(keyFileHash);
                }

                _keyManager.SetKey(key);
                UnlockSucceeded?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                CryptographicOperations.ZeroMemory(key);
                throw;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ErrorMessage = $"Unlock failed: {ex.Message}";
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passphraseBytes);
            IsUnlocking = false;
        }
    }

    private bool CanUnlock() => !string.IsNullOrEmpty(Passphrase) && !IsUnlocking;

    [RelayCommand]
    private async Task BrowseKeyFileAsync()
    {
        var path = await _picker.PickKeyFileAsync();
        if (path != null) KeyFilePath = path;
    }

    [RelayCommand]
    private void ClearKeyFile() => KeyFilePath = null;
}
