using CommunityToolkit.Mvvm.ComponentModel;

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

    public string FilePath { get; init; } = string.Empty;
    public bool IsFolder { get; init; }

    public string OperationIcon => Operation switch
    {
        FileOperation.Encrypt => IsFolder ? "📁" : "🔒",
        _ => "🔓"
    };

    public string StatusText => Status switch
    {
        FileStatus.Pending => "Pending",
        FileStatus.Processing => $"{Progress:F0}%",
        FileStatus.Done => Operation == FileOperation.Encrypt ? "Encrypted" : "Decrypted",
        FileStatus.Failed => $"Failed: {Error}",
        _ => string.Empty
    };

    partial void OnStatusChanged(FileStatus value) => OnPropertyChanged(nameof(StatusText));
    partial void OnProgressChanged(double value) => OnPropertyChanged(nameof(StatusText));
    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(StatusText));
}
