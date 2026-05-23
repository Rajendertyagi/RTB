using System;
using System.Collections.Generic;
using System.Linq;

namespace TB.Infrastructure;

public static class FuzzyMatcher
{
    public static List<string> Filter(IEnumerable<string> items, string query, int threshold = 2)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<string>();
        var q = query.ToLowerInvariant();
        return items.Where(i => !string.IsNullOrEmpty(i) && (i.ToLowerInvariant().Contains(q) || Levenshtein(q, i.ToLowerInvariant()) <= threshold))
                    .Take(10).ToList();
    }
    private static int Levenshtein(string s, string t)
    {
        if (s == t) return 0;
        var (m, n) = (s.Length, t.Length);
        var d = new int[m + 1, n + 1];
        for (int i = 0; i <= m; i++) d[i, 0] = i;
        for (int j = 0; j <= n; j++) d[0, j] = j;
        for (int i = 1; i <= m; i++) for (int j = 1; j <= n; j++)
            d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + (s[i - 1] == t[j - 1] ? 0 : 1));
        return d[m, n];
    }
}
