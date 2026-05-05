using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using quickLink.Models.ListItems;

namespace quickLink.Helpers
{
    public interface IUsageScoreProvider
    {
        double GetUsageScore(IListItem item);
    }

    public sealed class FilterOptions
    {
        public int MaxResults { get; init; } = 6;
        public bool IncludeInternalCommands { get; init; } = true;
        public double UsageWeight { get; init; } = 1.0;
        public double FuzzyWeight { get; init; } = 1.0;
    }

    /// <summary>
    /// Pure scoring + sorting pipeline for the launcher list.
    /// Extracted from MainWindow.FilterItems so it can be unit-tested without UI.
    /// </summary>
    public static class ItemFilter
    {
        public static List<IListItem> Filter(
            IReadOnlyList<IListItem> items,
            IReadOnlyList<IListItem> internalCommands,
            string query,
            IUsageScoreProvider usage,
            FilterOptions options,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(internalCommands);
            ArgumentNullException.ThrowIfNull(usage);
            ArgumentNullException.ThrowIfNull(options);

            ct.ThrowIfCancellationRequested();
            ResetHighlights(items);
            ResetHighlights(internalCommands);

            if (string.IsNullOrWhiteSpace(query))
            {
                ct.ThrowIfCancellationRequested();
                var sortedByUsage = items
                    .Select(i => (Item: i, Score: usage.GetUsageScore(i)))
                    .OrderByDescending(x => x.Score)
                    .Take(options.MaxResults)
                    .Select(x => x.Item)
                    .ToList();

                if (options.IncludeInternalCommands)
                {
                    foreach (var cmd in internalCommands)
                    {
                        if (sortedByUsage.Count >= options.MaxResults) break;
                        sortedByUsage.Add(cmd);
                    }
                }
                return sortedByUsage;
            }

            var scored = new List<(IListItem Item, double Score, int[]? Hl)>(
                items.Count + (options.IncludeInternalCommands ? internalCommands.Count : 0));

            int loopCounter = 0;
            foreach (var item in items)
            {
                if ((++loopCounter & 0x3F) == 0) ct.ThrowIfCancellationRequested();

                if (TryScore(item, query, options, usage.GetUsageScore(item), out var combined, out var hl))
                {
                    scored.Add((item, combined, hl));
                }
            }

            if (options.IncludeInternalCommands)
            {
                foreach (var cmd in internalCommands)
                {
                    if ((++loopCounter & 0x3F) == 0) ct.ThrowIfCancellationRequested();

                    if (TryScore(cmd, query, options, 0.0, out var combined, out var hl))
                    {
                        scored.Add((cmd, combined, hl));
                    }
                }
            }

            ct.ThrowIfCancellationRequested();

            var top = scored
                .OrderByDescending(x => x.Score)
                .Take(options.MaxResults)
                .ToList();

            foreach (var entry in top)
            {
                entry.Item.TitleHighlights = entry.Hl;
            }

            return top.Select(x => x.Item).ToList();
        }

        private static bool TryScore(
            IListItem item,
            string query,
            FilterOptions options,
            double usageScore,
            out double combinedScore,
            out int[]? highlights)
        {
            combinedScore = 0;
            highlights = null;

            var title = item.DisplayTitle ?? string.Empty;
            var value = item.DisplayValue ?? string.Empty;

            bool titleHit = FuzzyMatcher.TryMatch(query, title, out var titleResult);
            bool valueHit = FuzzyMatcher.TryMatch(query, value, out var valueResult);

            if (!titleHit && !valueHit)
                return false;

            int bestFuzzy;
            if (titleHit && valueHit)
            {
                if (titleResult.Score >= valueResult.Score)
                {
                    bestFuzzy = titleResult.Score;
                    highlights = titleResult.MatchedIndexes;
                }
                else
                {
                    bestFuzzy = valueResult.Score;
                    highlights = null;
                }
            }
            else if (titleHit)
            {
                bestFuzzy = titleResult.Score;
                highlights = titleResult.MatchedIndexes;
            }
            else
            {
                bestFuzzy = valueResult.Score;
                highlights = null;
            }

            combinedScore = options.FuzzyWeight * bestFuzzy + options.UsageWeight * usageScore;
            return true;
        }

        private static void ResetHighlights(IEnumerable<IListItem> items)
        {
            foreach (var item in items) item.TitleHighlights = null;
        }
    }
}
