using System.Security.Cryptography;

namespace Fcry.Core.Crypto;

public sealed class MasterKeyManager : IDisposable
{
    private byte[]? _masterKey;
    private bool _disposed;

    public bool IsUnlocked => _masterKey != null && !_disposed;

    public void SetKey(byte[] key)
    {
        if (_masterKey != null)
            CryptographicOperations.ZeroMemory(_masterKey);
        _masterKey = key;
    }

    public ReadOnlyMemory<byte> GetKey()
    {
        if (_masterKey == null || _disposed)
            throw new InvalidOperationException("Master key is not available.");
        return _masterKey;
    }

    public byte[] CopyKey()
    {
        if (_masterKey == null || _disposed)
            throw new InvalidOperationException("Master key is not available.");
        return _masterKey.ToArray();
    }

    public void Lock()
    {
        if (_masterKey != null)
        {
            CryptographicOperations.ZeroMemory(_masterKey);
            _masterKey = null;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Lock();
            _disposed = true;
        }
    }
}
