using System.Security.Cryptography;

namespace Fcry.Core.Crypto;

public static class AesGcmCipher
{
    public static void Encrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> plaintext,
        Span<byte> ciphertext,
        Span<byte> tag)
    {
        using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(iv, plaintext, ciphertext, tag);
    }

    public static bool TryDecrypt(
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        Span<byte> plaintext)
    {
        try
        {
            using var aes = new AesGcm(key, AesGcm.TagByteSizes.MaxSize);
            aes.Decrypt(iv, ciphertext, tag, plaintext);
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}
