using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Fcry.Core.Crypto;

public sealed class MasterKeyManager : IDisposable
{
    private byte[]? _masterKey;
    private GCHandle _keyPin;
    private bool _disposed;

    public bool IsUnlocked => _masterKey != null && !_disposed;

    public void SetKey(byte[] key)
    {
        Lock();
        _masterKey = key;
        _keyPin = GCHandle.Alloc(key, GCHandleType.Pinned);
        TryLockPages(_keyPin.AddrOfPinnedObject(), (nuint)key.Length);
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
            if (_keyPin.IsAllocated)
            {
                TryUnlockPages(_keyPin.AddrOfPinnedObject(), (nuint)_masterKey.Length);
                _keyPin.Free();
            }
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

    private static void TryLockPages(IntPtr addr, nuint len)
    {
        try
        {
            if (OperatingSystem.IsWindows()) VirtualLock(addr, len);
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) mlock(addr, len);
        }
        catch { }
    }

    private static void TryUnlockPages(IntPtr addr, nuint len)
    {
        try
        {
            if (OperatingSystem.IsWindows()) VirtualUnlock(addr, len);
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) munlock(addr, len);
        }
        catch { }
    }

    [DllImport("libc")] private static extern int mlock(IntPtr addr, nuint len);
    [DllImport("libc")] private static extern int munlock(IntPtr addr, nuint len);
    [DllImport("kernel32")] private static extern bool VirtualLock(IntPtr lpAddress, nuint dwSize);
    [DllImport("kernel32")] private static extern bool VirtualUnlock(IntPtr lpAddress, nuint dwSize);
}
