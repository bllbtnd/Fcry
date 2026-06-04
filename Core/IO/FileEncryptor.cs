using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Fcry.Core.Crypto;
using Fcry.Core.Models;

namespace Fcry.Core.IO;

public static class FileEncryptor
{
    public static async Task<CryptoResult> EncryptAsync(
        string sourcePath,
        string destPath,
        ReadOnlyMemory<byte> masterKey,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var plaintext = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
            progress?.Report(0.3);
            return await EncryptBytesAsync(
                plaintext, Path.GetFileName(sourcePath),
                destPath, masterKey, progress, 0.3, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return CryptoResult.Fail(ex.Message); }
    }

    public static async Task<CryptoResult> EncryptFolderAsync(
        string sourceFolderPath,
        string destPath,
        ReadOnlyMemory<byte> masterKey,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".zip");
        try
        {
            await Task.Run(() =>
                ZipFile.CreateFromDirectory(sourceFolderPath, tempZip,
                    CompressionLevel.Fastest, includeBaseDirectory: false),
                cancellationToken);
            progress?.Report(0.25);

            var plaintext = await File.ReadAllBytesAsync(tempZip, cancellationToken);
            progress?.Report(0.4);

            var folderName = Path.GetFileName(
                sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar,
                                         Path.AltDirectorySeparatorChar)) + "/";

            return await EncryptBytesAsync(
                plaintext, folderName,
                destPath, masterKey, progress, 0.4, cancellationToken);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return CryptoResult.Fail(ex.Message); }
        finally
        {
            try { File.Delete(tempZip); } catch { }
        }
    }

    private static async Task<CryptoResult> EncryptBytesAsync(
        byte[] plaintext,
        string storedName,
        string destPath,
        ReadOnlyMemory<byte> masterKey,
        IProgress<double>? progress,
        double progressOffset,
        CancellationToken cancellationToken)
    {
        var perFileSalt = new byte[FileHeader.SaltSize];
        var iv = new byte[FileHeader.IvSize];
        RandomNumberGenerator.Fill(perFileSalt.AsSpan());
        RandomNumberGenerator.Fill(iv.AsSpan());

        var fileKey = new byte[32];
        try
        {
            HkdfDerivation.DeriveFileKey(masterKey.Span, perFileSalt, fileKey);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[FileHeader.TagSize];
            await Task.Run(() => AesGcmCipher.Encrypt(fileKey, iv, plaintext, ciphertext, tag),
                cancellationToken);

            progress?.Report(progressOffset + 0.4);

            var nameBytes = Encoding.UTF8.GetBytes(storedName);
            var nameLenBytes = BitConverter.GetBytes((long)nameBytes.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(nameLenBytes);

            await using var output = new FileStream(destPath, FileMode.Create, FileAccess.Write);
            await output.WriteAsync(FileHeader.Magic, cancellationToken);
            output.WriteByte(FileHeader.CurrentVersion);
            await output.WriteAsync(perFileSalt, cancellationToken);
            await output.WriteAsync(iv, cancellationToken);
            await output.WriteAsync(nameLenBytes, cancellationToken);
            await output.WriteAsync(nameBytes, cancellationToken);
            await output.WriteAsync(ciphertext, cancellationToken);
            await output.WriteAsync(tag, cancellationToken);

            progress?.Report(1.0);
            return CryptoResult.Ok();
        }
        finally
        {
            CryptographicOperations.ZeroMemory(fileKey);
        }
    }
}
