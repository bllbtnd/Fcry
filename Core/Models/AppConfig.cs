namespace Fcry.Core.Models;

public sealed class AppConfig
{
    public byte[] ArgonSalt { get; set; } = [];
    public byte[]? PassphraseVerification { get; set; }
}
