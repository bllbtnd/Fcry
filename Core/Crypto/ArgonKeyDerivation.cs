using System.Security.Cryptography;
using Konscious.Security.Cryptography;

namespace Fcry.Core.Crypto;

public static class ArgonKeyDerivation
{
    private const int Iterations = 4;
    private const int MemoryKilobytes = 65536;
    private const int Parallelism = 2;
    private const int OutputLength = 32;

    public static byte[] DeriveKey(ReadOnlySpan<byte> passphrase, ReadOnlySpan<byte> salt)
    {
        var passwordBytes = passphrase.ToArray();
        var saltBytes = salt.ToArray();
        try
        {
            using var argon2 = new Argon2id(passwordBytes);
            argon2.Salt = saltBytes;
            argon2.Iterations = Iterations;
            argon2.MemorySize = MemoryKilobytes;
            argon2.DegreeOfParallelism = Parallelism;
            return argon2.GetBytes(OutputLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(saltBytes);
        }
    }
}
