using Avalonia.Data.Converters;
using Avalonia.Media;
using Fcry.App;
using Fcry.App.ViewModels;

namespace Fcry.App.Converters;

public static class AppConverters
{
    public static readonly FuncValueConverter<int, bool> CountGreaterThanZero =
        new(n => n > 0);

    public static readonly FuncValueConverter<FileStatus, IBrush> StatusToBrush =
        new(status => status switch
        {
            FileStatus.Done => new SolidColorBrush(Color.Parse("#30d158")),
            FileStatus.Failed => new SolidColorBrush(Color.Parse("#ff453a")),
            FileStatus.Processing => new SolidColorBrush(Color.Parse("#0a84ff")),
            _ => new SolidColorBrush(Color.Parse("#636366"))
        });

    public static readonly FuncValueConverter<FileStatus, bool> StatusIsProcessing =
        new(status => status == FileStatus.Processing);

    public static readonly FuncValueConverter<string, Geometry?> IconKeyToGeometry =
        new(key => key switch
        {
            "lock" => Icons.Lock,
            "unlock" => Icons.Unlock,
            "folder" => Icons.Folder,
            _ => null
        });
}
