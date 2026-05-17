namespace Fcry.Core.Models;

public static class FileHeader
{
    public static readonly byte[] Magic = [0x46, 0x43, 0x52, 0x59];
    public const byte CurrentVersion = 0x01;
    public const int MagicSize = 4;
    public const int SaltSize = 32;
    public const int IvSize = 12;
    public const int TagSize = 16;
    public const int FilenameLengthSize = 8;
    public const int FixedHeaderSize = MagicSize + 1 + SaltSize + IvSize + FilenameLengthSize;
}
