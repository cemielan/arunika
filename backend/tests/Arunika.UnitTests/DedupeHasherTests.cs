using Arunika.Application.Services;

namespace Arunika.UnitTests;

public class DedupeHasherTests
{
    [Fact]
    public void ComputeHash_SameTitleAndSource_ProducesSameHash()
    {
        var hash1 = DedupeHasher.ComputeHash("Fed keeps interest rates unchanged", "Reuters");
        var hash2 = DedupeHasher.ComputeHash("Fed keeps interest rates unchanged", "Reuters");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IsCaseInsensitive()
    {
        var hash1 = DedupeHasher.ComputeHash("Fed Keeps Interest Rates Unchanged", "Reuters");
        var hash2 = DedupeHasher.ComputeHash("fed keeps interest rates unchanged", "REUTERS");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_IgnoresLeadingAndTrailingWhitespace()
    {
        var hash1 = DedupeHasher.ComputeHash("  Fed keeps interest rates unchanged  ", "  Reuters  ");
        var hash2 = DedupeHasher.ComputeHash("Fed keeps interest rates unchanged", "Reuters");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentTitles_ProduceDifferentHashes()
    {
        var hash1 = DedupeHasher.ComputeHash("Fed keeps interest rates unchanged", "Reuters");
        var hash2 = DedupeHasher.ComputeHash("Oil prices rise 4%", "Reuters");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_SameTitleDifferentSource_ProducesDifferentHashes()
    {
        // Design doc §6 step 1: the hash is over (title + source), so the same
        // wire story republished under a different outlet name is intentionally
        // NOT an exact-hash match — it falls through to the title-similarity check instead.
        var hash1 = DedupeHasher.ComputeHash("Fed keeps interest rates unchanged", "Reuters");
        var hash2 = DedupeHasher.ComputeHash("Fed keeps interest rates unchanged", "AP");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_ReturnsUppercaseHexString()
    {
        var hash = DedupeHasher.ComputeHash("Title", "Source");

        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9A-F]{64}$", hash);
    }
}
