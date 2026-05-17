using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace Fcry.Core.Crypto;

public static class ArgonKeyDerivation
{
    public static byte[] DeriveKey(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> salt)
    {
        var passwordBytes = passphrase.ToArray();
        var saltBytes = salt.ToArray();
        try
        {
            using var argon2 = new Argon2id(passwordBytes);
            argon2.Salt = saltBytes;
            argon2.Iterations = 4;
            argon2.MemorySize = 65536;
            argon2.DegreeOfParallelism = 2;
            return argon2.GetBytes(32);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(saltBytes);
        }
    }
}
