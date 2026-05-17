namespace Fcry.Core.Models;

public sealed record CryptoResult(bool Success, string? Error = null)
{
    public static CryptoResult Ok() => new(true);
    public static CryptoResult Fail(string error) => new(false, error);
}
