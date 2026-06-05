using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Fcry.App.ViewModels;

public enum FileOperation { Encrypt, Decrypt }

public enum FileStatus { Pending, Processing, Done, Failed }

public sealed partial class FileQueueItem : ObservableObject
{
    [ObservableProperty] private string _fileName = string.Empty;
    [ObservableProperty] private FileOperation _operation;
    [ObservableProperty] private FileStatus _status = FileStatus.Pending;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _error;
    [ObservableProperty] private string? _outputPath;

    public string FilePath { get; init; } = string.Empty;
    public bool IsFolder { get; init; }

    public string OperationIcon => Operation switch
    {
        FileOperation.Encrypt => IsFolder ? "folder" : "lock",
        _ => "unlock"
    };

    public string StatusText => Status switch
    {
        FileStatus.Pending => "Pending",
        FileStatus.Processing => $"{Progress:F0}%",
        FileStatus.Done => Operation == FileOperation.Encrypt ? "Encrypted" : "Decrypted",
        FileStatus.Failed => $"Failed: {Error}",
        _ => string.Empty
    };

    [RelayCommand(CanExecute = nameof(CanReveal))]
    private void RevealInExplorer()
    {
        if (OutputPath == null) return;
        try
        {
            if (OperatingSystem.IsMacOS())
                Process.Start("open", $"-R \"{OutputPath}\"");
            else if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{OutputPath}\"")
                    { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("xdg-open", Path.GetDirectoryName(OutputPath) ?? OutputPath)
                    { UseShellExecute = true });
        }
        catch { }
    }

    private bool CanReveal() => OutputPath != null && Status == FileStatus.Done;

    partial void OnStatusChanged(FileStatus value)
    {
        OnPropertyChanged(nameof(StatusText));
        RevealInExplorerCommand.NotifyCanExecuteChanged();
    }

    partial void OnProgressChanged(double value) => OnPropertyChanged(nameof(StatusText));
    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(StatusText));

    partial void OnOutputPathChanged(string? value) =>
        RevealInExplorerCommand.NotifyCanExecuteChanged();
}
