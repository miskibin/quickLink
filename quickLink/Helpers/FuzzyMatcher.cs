using System;

namespace quickLink.Helpers
{
    public readonly record struct FuzzyResult(int Score, int[] MatchedIndexes)
    {
        public static FuzzyResult None => new(0, Array.Empty<int>());
    }

    public static class FuzzyMatcher
    {
        private const int PrefixBonus = 15;
        private const int WordBoundaryBonus = 10;
        private const int ConsecutiveBonus = 5;
        private const int CaseMatchBonus = 1;
        private const int GapPenalty = -1;

        public static bool TryMatch(string query, string text, out FuzzyResult result)
        {
            result = FuzzyResult.None;

            if (string.IsNullOrEmpty(query))
                return false;

            if (string.IsNullOrEmpty(text) || query.Length > text.Length)
                return false;

            var indexes = new int[query.Length];
            int score = 0;
            int textIndex = 0;
            int prevMatchIndex = -2;

            for (int qi = 0; qi < query.Length; qi++)
            {
                char qc = query[qi];
                char qcLower = char.ToLowerInvariant(qc);
                int found = -1;

                for (int ti = textIndex; ti < text.Length; ti++)
                {
                    char tc = text[ti];
                    if (char.ToLowerInvariant(tc) == qcLower)
                    {
                        found = ti;
                        if (qc == tc)
                            score += CaseMatchBonus;
                        break;
                    }
                }

                if (found < 0)
                    return false;

                if (found == 0 && qi == 0)
                    score += PrefixBonus;

                if (IsWordBoundary(text, found))
                    score += WordBoundaryBonus;

                if (found == prevMatchIndex + 1)
                    score += ConsecutiveBonus;
                else if (qi > 0)
                    score += GapPenalty * (found - prevMatchIndex - 1);

                indexes[qi] = found;
                prevMatchIndex = found;
                textIndex = found + 1;
            }

            result = new FuzzyResult(score, indexes);
            return true;
        }

        private static bool IsWordBoundary(string text, int index)
        {
            if (index == 0) return true;
            char curr = text[index];
            char prev = text[index - 1];

            if (prev == ' ' || prev == '_' || prev == '-' || prev == '/' || prev == '.' || prev == '\\')
                return true;

            if (char.IsUpper(curr) && char.IsLower(prev))
                return true;

            return false;
        }
    }
}
