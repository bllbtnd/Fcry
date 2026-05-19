using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fcry.Core.Crypto;
using Fcry.Core.IO;

namespace Fcry.App.ViewModels;

public sealed partial class MainScreenViewModel : ViewModelBase, IDisposable
{
    private readonly MasterKeyManager _keyManager;
    private readonly System.Threading.Timer _inactivityTimer;
    private DateTime _lastActivity;
    private bool _processingQueue;
    private const int TimeoutSeconds = 300;
    private const int CountdownThreshold = 60;

    [ObservableProperty] private ObservableCollection<FileQueueItem> _fileQueue = [];
    [ObservableProperty] private int? _countdownSeconds;
    [ObservableProperty] private bool _isDragOver;

    public event EventHandler? LockRequested;

    public MainScreenViewModel(MasterKeyManager keyManager)
    {
        _keyManager = keyManager;
        _lastActivity = DateTime.UtcNow;
        _inactivityTimer = new System.Threading.Timer(OnInactivityTick, null, 1000, 1000);
    }

    public void ResetInactivityTimer()
    {
        _lastActivity = DateTime.UtcNow;
        if (CountdownSeconds.HasValue)
            CountdownSeconds = null;
    }

    private void OnInactivityTick(object? state)
    {
        var remaining = TimeoutSeconds - (DateTime.UtcNow - _lastActivity).TotalSeconds;

        if (remaining <= 0)
        {
            _inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
            Dispatcher.UIThread.Post(() => LockRequested?.Invoke(this, EventArgs.Empty));
            return;
        }

        var displaySeconds = remaining <= CountdownThreshold ? (int)Math.Ceiling(remaining) : (int?)null;
        if (displaySeconds != CountdownSeconds)
            Dispatcher.UIThread.Post(() => CountdownSeconds = displaySeconds);
    }

    [RelayCommand]
    private void Lock()
    {
        _inactivityTimer.Change(Timeout.Infinite, Timeout.Infinite);
        LockRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ClearQueue()
    {
        var done = FileQueue.Where(i => i.Status is FileStatus.Done or FileStatus.Failed).ToList();
        foreach (var item in done)
            FileQueue.Remove(item);
    }

    public async Task EnqueueFilesAsync(IEnumerable<string> paths)
    {
        ResetInactivityTimer();

        foreach (var path in paths)
        {
            var isFcry = FileDecryptor.IsFcryFile(path);
            FileQueue.Add(new FileQueueItem
            {
                FileName = Path.GetFileName(path),
                FilePath = path,
                Operation = isFcry ? FileOperation.Decrypt : FileOperation.Encrypt
            });
        }

        if (!_processingQueue)
            await DrainQueueAsync();
    }

    private async Task DrainQueueAsync()
    {
        _processingQueue = true;
        try
        {
            while (true)
            {
                var next = FileQueue.FirstOrDefault(i => i.Status == FileStatus.Pending);
                if (next == null) break;
                await ProcessItemAsync(next);
            }
        }
        finally
        {
            _processingQueue = false;
        }
    }

    private async Task ProcessItemAsync(FileQueueItem item)
    {
        item.Status = FileStatus.Processing;
        var progress = new Progress<double>(p =>
        {
            item.Progress = p * 100;
            item.OnPropertyChanged(nameof(item.StatusText));
        });

        var masterKeyBytes = _keyManager.CopyKey();
        try
        {
            var masterKey = masterKeyBytes.AsMemory();
            Core.Models.CryptoResult result;

            if (item.Operation == FileOperation.Encrypt)
            {
                var destPath = item.FilePath + ".fcry";
                result = await FileEncryptor.EncryptAsync(item.FilePath, destPath, masterKey, progress);
            }
            else
            {
                var destDir = Path.GetDirectoryName(item.FilePath) ?? ".";
                result = await FileDecryptor.DecryptAsync(item.FilePath, destDir, masterKey, progress);
            }

            if (result.Success)
            {
                item.Progress = 100;
                item.Status = FileStatus.Done;
            }
            else
            {
                item.Status = FileStatus.Failed;
                item.Error = result.Error;
            }
        }
        catch (Exception ex)
        {
            item.Status = FileStatus.Failed;
            item.Error = ex.Message;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(masterKeyBytes);
        }
    }

    public void Dispose()
    {
        _inactivityTimer.Dispose();
    }
}
