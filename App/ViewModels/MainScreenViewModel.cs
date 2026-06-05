using System.Collections.ObjectModel;
using System.Security.Cryptography;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Fcry.App.Services;
using Fcry.Core.Crypto;
using Fcry.Core.IO;

namespace Fcry.App.ViewModels;

public sealed partial class MainScreenViewModel : ViewModelBase, IDisposable
{
    private readonly MasterKeyManager _keyManager;
    private readonly IPickerService _picker;
    private readonly System.Threading.Timer _inactivityTimer;
    private CancellationTokenSource _cts = new();
    private DateTime _lastActivity;
    private bool _processingQueue;
    private const int TimeoutSeconds = 300;
    private const int CountdownThreshold = 60;

    [ObservableProperty] private ObservableCollection<FileQueueItem> _fileQueue = [];
    [ObservableProperty] private int? _countdownSeconds;
    [ObservableProperty] private bool _isDragOver;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private string? _outputDirectory;

    public bool HasFiles => FileQueue.Count > 0;

    public string OutputDirectoryDisplay =>
        OutputDirectory != null
            ? Path.GetFileName(OutputDirectory.TrimEnd(Path.DirectorySeparatorChar,
                                                        Path.AltDirectorySeparatorChar))
            : "same as source";

    public event EventHandler? LockRequested;

    public MainScreenViewModel(MasterKeyManager keyManager, IPickerService picker)
    {
        _keyManager = keyManager;
        _picker = picker;
        _lastActivity = DateTime.UtcNow;
        _inactivityTimer = new System.Threading.Timer(OnInactivityTick, null, 1000, 1000);
        FileQueue.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasFiles));
    }

    partial void OnOutputDirectoryChanged(string? value) =>
        OnPropertyChanged(nameof(OutputDirectoryDisplay));

    public void ResetInactivityTimer()
    {
        _lastActivity = DateTime.UtcNow;
        if (CountdownSeconds.HasValue) CountdownSeconds = null;
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
        var display = remaining <= CountdownThreshold ? (int)Math.Ceiling(remaining) : (int?)null;
        if (display != CountdownSeconds)
            Dispatcher.UIThread.Post(() => CountdownSeconds = display);
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
        foreach (var item in FileQueue.Where(i => i.Status is FileStatus.Done or FileStatus.Failed).ToList())
            FileQueue.Remove(item);
    }

    [RelayCommand]
    private void CancelProcessing()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        foreach (var item in FileQueue.Where(i => i.Status == FileStatus.Pending).ToList())
        {
            item.Status = FileStatus.Failed;
            item.Error = "Cancelled.";
        }
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        ResetInactivityTimer();
        var paths = await _picker.PickFilesAsync();
        if (paths.Count > 0) await EnqueueFilesAsync(paths);
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        ResetInactivityTimer();
        var path = await _picker.PickFolderAsync();
        if (path != null) await EnqueueFilesAsync([path]);
    }

    [RelayCommand]
    private async Task ChangeOutputDirectoryAsync()
    {
        var path = await _picker.PickOutputFolderAsync();
        if (path != null) OutputDirectory = path;
    }

    [RelayCommand]
    private void ClearOutputDirectory() => OutputDirectory = null;

    public async Task EnqueueFilesAsync(IEnumerable<string> paths)
    {
        ResetInactivityTimer();

        foreach (var path in paths)
        {
            var isFolder = Directory.Exists(path);
            var isFcry = !isFolder && FileDecryptor.IsFcryFile(path);
            var displayName = isFolder
                ? Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar,
                                               Path.AltDirectorySeparatorChar)) + "/"
                : Path.GetFileName(path);
            FileQueue.Add(new FileQueueItem
            {
                FileName = displayName,
                FilePath = path,
                IsFolder = isFolder,
                Operation = isFcry ? FileOperation.Decrypt : FileOperation.Encrypt
            });
        }

        if (!_processingQueue)
            await DrainQueueAsync();
    }

    private async Task DrainQueueAsync()
    {
        _processingQueue = true;
        IsProcessing = true;
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
            IsProcessing = false;
        }
    }

    private async Task ProcessItemAsync(FileQueueItem item)
    {
        if (!_keyManager.IsUnlocked)
        {
            item.Status = FileStatus.Failed;
            item.Error = "Session locked.";
            return;
        }

        item.Status = FileStatus.Processing;
        var progress = new Progress<double>(p => item.Progress = p * 100);

        byte[] masterKeyBytes;
        try { masterKeyBytes = _keyManager.CopyKey(); }
        catch (InvalidOperationException)
        {
            item.Status = FileStatus.Failed;
            item.Error = "Session locked.";
            return;
        }

        try
        {
            var token = _cts.Token;
            var masterKey = masterKeyBytes.AsMemory();
            Core.Models.CryptoResult result;

            if (item.Operation == FileOperation.Encrypt)
            {
                var sourceBase = item.FilePath.TrimEnd(Path.DirectorySeparatorChar,
                                                        Path.AltDirectorySeparatorChar);
                var outDir = OutputDirectory ?? Path.GetDirectoryName(sourceBase) ?? ".";
                var destName = Path.GetFileName(sourceBase) + ".fcry";
                var destPath = Path.Combine(outDir, destName);

                result = item.IsFolder
                    ? await FileEncryptor.EncryptFolderAsync(item.FilePath, destPath, masterKey, progress, token)
                    : await FileEncryptor.EncryptAsync(item.FilePath, destPath, masterKey, progress, token);
            }
            else
            {
                var outDir = OutputDirectory ?? Path.GetDirectoryName(item.FilePath) ?? ".";
                result = await FileDecryptor.DecryptAsync(item.FilePath, outDir, masterKey, progress, token);
            }

            if (result.Success)
            {
                item.Progress = 100;
                item.Status = FileStatus.Done;
                item.OutputPath = result.OutputPath;
            }
            else
            {
                item.Status = FileStatus.Failed;
                item.Error = result.Error;
            }
        }
        catch (OperationCanceledException)
        {
            item.Status = FileStatus.Failed;
            item.Error = "Cancelled.";
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
        _cts.Dispose();
    }
}
