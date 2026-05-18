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
                AesGcmCipher.Encrypt(fileKey, iv, plaintext, ciphertext, tag);

                progress?.Report(0.7);

                var fileName = Path.GetFileName(sourcePath);
                var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                var fileNameLenBytes = BitConverter.GetBytes((long)fileNameBytes.Length);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(fileNameLenBytes);

                await using var output = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                await output.WriteAsync(FileHeader.Magic, cancellationToken);
                output.WriteByte(FileHeader.CurrentVersion);
                await output.WriteAsync(perFileSalt, cancellationToken);
                await output.WriteAsync(iv, cancellationToken);
                await output.WriteAsync(fileNameLenBytes, cancellationToken);
                await output.WriteAsync(fileNameBytes, cancellationToken);
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
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return CryptoResult.Fail(ex.Message);
        }
    }
}
