using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using quickLink.Helpers;
using quickLink.Models.ListItems;
using Xunit;

namespace quickLink.Tests;

public class ItemFilterTests
{
    private sealed class FakeItem : IListItem
    {
        public FakeItem(string title, string value = "")
        {
            DisplayTitle = title;
            DisplayValue = value;
        }

        public string DisplayTitle { get; }
        public string DisplayValue { get; }
        public string IconGlyph => string.Empty;
        public string IconColor => string.Empty;
        public bool ShowEditButton => false;
        public bool ShowDeleteButton => false;
        public bool SupportsAutocomplete => false;
        public string AutocompleteText => string.Empty;
        public int[]? TitleHighlights { get; set; }
        public Task ExecuteAsync(IExecutionContext context) => Task.CompletedTask;
        public bool MatchesSearch(string searchText) => true;
    }

    private sealed class UsageStub : IUsageScoreProvider
    {
        private readonly Dictionary<IListItem, double> _scores = new();
        public UsageStub Set(IListItem item, double score) { _scores[item] = score; return this; }
        public double GetUsageScore(IListItem item) => _scores.TryGetValue(item, out var s) ? s : 0;
    }

    private static FilterOptions DefaultOptions => new() { MaxResults = 6, IncludeInternalCommands = true };

    [Fact]
    public void EmptyQueryReturnsItemsSortedByUsage()
    {
        var rare = new FakeItem("Rare");
        var popular = new FakeItem("Popular");
        var usage = new UsageStub().Set(rare, 0).Set(popular, 5);

        var result = ItemFilter.Filter(
            new IListItem[] { rare, popular },
            Array.Empty<IListItem>(),
            string.Empty, usage, DefaultOptions);

        Assert.Equal(new IListItem[] { popular, rare }, result);
    }

    [Fact]
    public void QueryFiltersOutNonMatches()
    {
        var chrome = new FakeItem("Chrome");
        var firefox = new FakeItem("Firefox");
        var usage = new UsageStub();

        var result = ItemFilter.Filter(
            new IListItem[] { chrome, firefox },
            Array.Empty<IListItem>(),
            "chr", usage, DefaultOptions);

        Assert.Single(result);
        Assert.Same(chrome, result[0]);
    }

    [Fact]
    public void HighlightsAreSetOnMatchedItems()
    {
        var chrome = new FakeItem("Chrome");
        var usage = new UsageStub();

        var result = ItemFilter.Filter(
            new IListItem[] { chrome }, Array.Empty<IListItem>(),
            "chr", usage, DefaultOptions);

        Assert.NotNull(result[0].TitleHighlights);
        Assert.Equal(new[] { 0, 1, 2 }, result[0].TitleHighlights);
    }

    [Fact]
    public void HighlightsAreClearedForUnmatchedItems()
    {
        var chrome = new FakeItem("Chrome") { TitleHighlights = new[] { 99 } };
        var usage = new UsageStub();

        ItemFilter.Filter(new IListItem[] { chrome }, Array.Empty<IListItem>(),
            "xyz", usage, DefaultOptions);

        Assert.Null(chrome.TitleHighlights);
    }

    [Fact]
    public void UsageBoostsCloseScores()
    {
        var typed = new FakeItem("ChrXome");
        var familiar = new FakeItem("Chrome");
        var usage = new UsageStub().Set(familiar, 10);

        var result = ItemFilter.Filter(
            new IListItem[] { typed, familiar }, Array.Empty<IListItem>(),
            "chr", usage,
            new FilterOptions { MaxResults = 6, IncludeInternalCommands = true, UsageWeight = 1.0, FuzzyWeight = 1.0 });

        Assert.Same(familiar, result[0]);
    }

    [Fact]
    public void RespectsMaxResults()
    {
        var items = Enumerable.Range(0, 20).Select(i => new FakeItem($"Item{i}")).Cast<IListItem>().ToList();

        var result = ItemFilter.Filter(items, Array.Empty<IListItem>(),
            "item", new UsageStub(),
            new FilterOptions { MaxResults = 4, IncludeInternalCommands = true });

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void IncludesInternalCommandsWhenFlagSet()
    {
        var item = new FakeItem("Foo");
        var settings = new FakeItem("Settings");
        var usage = new UsageStub();

        var result = ItemFilter.Filter(
            new IListItem[] { item }, new IListItem[] { settings },
            "set", usage, DefaultOptions);

        Assert.Contains(settings, result);
    }

    [Fact]
    public void SkipsInternalCommandsWhenFlagOff()
    {
        var item = new FakeItem("Foo");
        var settings = new FakeItem("Settings");

        var result = ItemFilter.Filter(
            new IListItem[] { item }, new IListItem[] { settings },
            "set", new UsageStub(),
            new FilterOptions { MaxResults = 6, IncludeInternalCommands = false });

        Assert.DoesNotContain(settings, result);
    }

    [Fact]
    public void CancelsWhenTokenIsTriggered()
    {
        var items = Enumerable.Range(0, 1000).Select(i => new FakeItem($"Item{i}")).Cast<IListItem>().ToList();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            ItemFilter.Filter(items, Array.Empty<IListItem>(), "x", new UsageStub(), DefaultOptions, cts.Token));
    }

    [Fact]
    public void MatchesAgainstDisplayValueAsFallback()
    {
        var item = new FakeItem("My link", "github.com/repo");
        var result = ItemFilter.Filter(
            new IListItem[] { item }, Array.Empty<IListItem>(),
            "github", new UsageStub(), DefaultOptions);

        Assert.Single(result);
        Assert.Same(item, result[0]);
    }
}
