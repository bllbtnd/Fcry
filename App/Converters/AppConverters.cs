using Avalonia.Data.Converters;
using Avalonia.Media;
using Fcry.App.ViewModels;

namespace Fcry.App.Converters;

public static class AppConverters
{
    public static readonly FuncValueConverter<int, bool> CountGreaterThanZero =
        new(n => n > 0);

    public static readonly FuncValueConverter<FileStatus, IBrush> StatusToBrush =
        new(status => status switch
        {
            FileStatus.Done => new SolidColorBrush(Color.Parse("#44cc88")),
            FileStatus.Failed => new SolidColorBrush(Color.Parse("#e94560")),
            FileStatus.Processing => new SolidColorBrush(Color.Parse("#8888cc")),
            _ => new SolidColorBrush(Color.Parse("#555577"))
        });

    public static readonly FuncValueConverter<FileStatus, bool> StatusIsProcessing =
        new(status => status == FileStatus.Processing);
}
