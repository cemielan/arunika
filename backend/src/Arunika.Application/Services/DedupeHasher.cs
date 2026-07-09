using System.Security.Cryptography;
using System.Text;

namespace Arunika.Application.Services;

/// <summary>
/// Computes the exact-match dedupe hash from a normalized title + source name
/// (design doc §6, step 1). Used both when persisting a new article and later,
/// in Phase 4, as the cheapest first check before the title-similarity pass.
/// </summary>
public static class DedupeHasher
{
    public static string ComputeHash(string title, string sourceName)
    {
        var normalized = $"{title.Trim().ToLowerInvariant()}|{sourceName.Trim().ToLowerInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes);
    }
}
