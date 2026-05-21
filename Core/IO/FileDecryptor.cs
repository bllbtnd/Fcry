using System.Security.Cryptography;
using System.Text;
using Fcry.Core.Crypto;
using Fcry.Core.Models;

namespace Fcry.Core.IO;

public static class FileDecryptor
{
    public static async Task<CryptoResult> DecryptAsync(
        string sourcePath,
        string destDirectory,
        ReadOnlyMemory<byte> masterKey,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
            progress?.Report(0.3);

            var span = data.AsSpan();

            if (span.Length < FileHeader.FixedHeaderSize)
                return CryptoResult.Fail("File is too short to be a valid Fcry file.");

            if (!span[..FileHeader.MagicSize].SequenceEqual(FileHeader.Magic))
                return CryptoResult.Fail("Not a valid Fcry file.");

            var version = span[FileHeader.MagicSize];
            if (version != FileHeader.CurrentVersion)
                return CryptoResult.Fail($"Unsupported file version: 0x{version:X2}");

            var offset = FileHeader.MagicSize + 1;
            var perFileSalt = span.Slice(offset, FileHeader.SaltSize);
            offset += FileHeader.SaltSize;

            var iv = span.Slice(offset, FileHeader.IvSize);
            offset += FileHeader.IvSize;

            var nameLenBytes = span.Slice(offset, FileHeader.FilenameLengthSize).ToArray();
            if (BitConverter.IsLittleEndian)
                Array.Reverse(nameLenBytes);
            var nameLen = (int)BitConverter.ToInt64(nameLenBytes, 0);
            offset += FileHeader.FilenameLengthSize;

            if (nameLen < 0 || nameLen > 4096 || offset + nameLen > span.Length)
                return CryptoResult.Fail("Invalid file header.");

            var fileName = Encoding.UTF8.GetString(span.Slice(offset, nameLen));
            offset += nameLen;

            var remaining = span[offset..];
            if (remaining.Length < FileHeader.TagSize)
                return CryptoResult.Fail("File is truncated.");

            var tag = remaining[^FileHeader.TagSize..];
            var ciphertext = remaining[..^FileHeader.TagSize];

            var fileKey = new byte[32];
            var plaintext = new byte[ciphertext.Length];
            try
            {
                HkdfDerivation.DeriveFileKey(masterKey.Span, perFileSalt, fileKey);

                if (!AesGcmCipher.TryDecrypt(fileKey, iv, ciphertext, tag, plaintext))
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                    return CryptoResult.Fail("Decryption failed. Wrong passphrase or corrupted file.");
                }

                progress?.Report(0.8);

                var destPath = Path.Combine(destDirectory, fileName);
                await File.WriteAllBytesAsync(destPath, plaintext, cancellationToken);

                progress?.Report(1.0);
                return CryptoResult.Ok();
            }
            finally
            {
                CryptographicOperations.ZeroMemory(fileKey);
                CryptographicOperations.ZeroMemory(plaintext);
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

    public static bool IsFcryFile(string path)
    {
        try
        {
            using var fs = File.OpenRead(path);
            Span<byte> magic = stackalloc byte[FileHeader.MagicSize];
            return fs.Read(magic) == FileHeader.MagicSize && magic.SequenceEqual(FileHeader.Magic);
        }
        catch
        {
            return false;
        }
    }
}
