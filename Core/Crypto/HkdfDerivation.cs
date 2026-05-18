using System.Security.Cryptography;

namespace Fcry.Core.Crypto;

public static class HkdfDerivation
{
    public static void DeriveFileKey(ReadOnlySpan<byte> masterKey, ReadOnlySpan<byte> perFileSalt, Span<byte> output)
    {
        HKDF.DeriveKey(HashAlgorithmName.SHA256, masterKey, output, ReadOnlySpan<byte>.Empty, perFileSalt);
    }
}
