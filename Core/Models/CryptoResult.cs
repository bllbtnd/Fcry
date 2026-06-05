namespace Fcry.Core.Models;

public sealed record CryptoResult(bool Success, string? Error = null, string? OutputPath = null)
{
    public static CryptoResult Ok(string outputPath) => new(true, null, outputPath);
    public static CryptoResult Fail(string error) => new(false, error);
}
