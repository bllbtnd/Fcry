using System.Security.Cryptography;

namespace Fcry.Core.IO;

public static class SecureDelete
{
    public static void Delete(string path, bool isFolder)
    {
        if (isFolder)
        {
            if (!Directory.Exists(path)) return;
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                ShredFile(file);
            Directory.Delete(path, recursive: true);
        }
        else
        {
            ShredFile(path);
        }
    }

    private static void ShredFile(string path)
    {
        if (!File.Exists(path)) return;
        try
        {
            var info = new FileInfo(path) { Attributes = FileAttributes.Normal };
            var length = info.Length;

            if (length > 0)
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None);
                var buffer = new byte[81920];
                var remaining = length;
                while (remaining > 0)
                {
                    var chunk = (int)Math.Min(buffer.Length, remaining);
                    RandomNumberGenerator.Fill(buffer.AsSpan(0, chunk));
                    fs.Write(buffer, 0, chunk);
                    remaining -= chunk;
                }
                fs.Flush(true);
            }

            var temp = Path.Combine(Path.GetDirectoryName(path)!, Path.GetRandomFileName());
            File.Move(path, temp);
            File.Delete(temp);
        }
        catch
        {
            try { File.Delete(path); } catch { }
        }
    }
}
