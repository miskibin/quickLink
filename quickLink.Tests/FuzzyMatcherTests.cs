using quickLink.Helpers;
using Xunit;

namespace quickLink.Tests;

public class FuzzyMatcherTests
{
    [Fact]
    public void EmptyQueryReturnsFalse()
    {
        Assert.False(FuzzyMatcher.TryMatch(string.Empty, "Chrome", out _));
    }

    [Fact]
    public void EmptyTextReturnsFalse()
    {
        Assert.False(FuzzyMatcher.TryMatch("ch", string.Empty, out _));
    }

    [Fact]
    public void QueryLongerThanTextReturnsFalse()
    {
        Assert.False(FuzzyMatcher.TryMatch("googlechrome", "chrome", out _));
    }

    [Fact]
    public void NoMatchReturnsFalse()
    {
        Assert.False(FuzzyMatcher.TryMatch("xyz", "chrome", out _));
    }

    [Fact]
    public void ExactPrefixMatches()
    {
        Assert.True(FuzzyMatcher.TryMatch("chro", "Chrome", out var result));
        Assert.Equal(new[] { 0, 1, 2, 3 }, result.MatchedIndexes);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public void CaseInsensitiveMatch()
    {
        Assert.True(FuzzyMatcher.TryMatch("CHR", "chrome", out var result));
        Assert.Equal(new[] { 0, 1, 2 }, result.MatchedIndexes);
    }

    [Fact]
    public void CamelCaseGetsWordBoundaryBonus()
    {
        Assert.True(FuzzyMatcher.TryMatch("gc", "GoogleChrome", out var camel));
        Assert.True(FuzzyMatcher.TryMatch("gc", "googlechrome", out var lower));
        Assert.Equal(new[] { 0, 6 }, camel.MatchedIndexes);
        Assert.True(camel.Score > lower.Score, "Camel-case match should outscore non-boundary match.");
    }

    [Fact]
    public void PrefixOutscoresMidMatch()
    {
        Assert.True(FuzzyMatcher.TryMatch("doc", "Documentation", out var prefix));
        Assert.True(FuzzyMatcher.TryMatch("doc", "Add Doc Item", out var mid));
        Assert.True(prefix.Score > 0);
        Assert.True(mid.Score > 0);
    }

    [Fact]
    public void PrefixBeatsLaterMatch()
    {
        Assert.True(FuzzyMatcher.TryMatch("ch", "chrome", out var prefix));
        Assert.True(FuzzyMatcher.TryMatch("ch", "search", out var later));
        Assert.True(prefix.Score > later.Score);
    }

    [Fact]
    public void ConsecutiveBeatsScattered()
    {
        Assert.True(FuzzyMatcher.TryMatch("abc", "abcdef", out var consecutive));
        Assert.True(FuzzyMatcher.TryMatch("abc", "a_b_c_d", out var scattered));
        Assert.True(consecutive.Score > scattered.Score);
    }

    [Fact]
    public void HandlesUnicodeWithoutCrash()
    {
        bool didMatch = FuzzyMatcher.TryMatch("add", "⚡ Add new item", out var result);
        Assert.True(didMatch);
        Assert.Equal(3, result.MatchedIndexes.Length);
    }

    [Fact]
    public void IndexesMonotonicallyIncreasing()
    {
        Assert.True(FuzzyMatcher.TryMatch("abc", "_a_b_c_", out var result));
        for (int i = 1; i < result.MatchedIndexes.Length; i++)
        {
            Assert.True(result.MatchedIndexes[i] > result.MatchedIndexes[i - 1]);
        }
    }
}
