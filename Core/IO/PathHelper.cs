namespace Fcry.Core.IO;

public static class PathHelper
{
    public static string FindAvailable(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path)) return path;

        var dir = Path.GetDirectoryName(path) ?? ".";
        var ext = Path.GetExtension(path);
        var nameNoExt = Path.GetFileNameWithoutExtension(path);

        var secondExt = ext.Equals(".fcry", StringComparison.OrdinalIgnoreCase)
            ? Path.GetExtension(nameNoExt)
            : string.Empty;
        var baseName = string.IsNullOrEmpty(secondExt)
            ? nameNoExt
            : Path.GetFileNameWithoutExtension(nameNoExt);
        var fullExt = secondExt + ext;

        for (var i = 1; i <= 999; i++)
        {
            var candidate = Path.Combine(dir, $"{baseName} ({i}){fullExt}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
        }

        return path;
    }
}
