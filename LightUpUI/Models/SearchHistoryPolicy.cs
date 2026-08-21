using System;
using System.Collections.Generic;
using System.Linq;

namespace LightUpUI.Models;

public static class SearchHistoryPolicy
{
    public const int MaxEntries = 20;

    public static void Record(IList<string> history, string? query)
    {
        var normalized = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        for (var index = history.Count - 1; index >= 0; index--)
        {
            if (string.Equals(history[index], normalized, StringComparison.OrdinalIgnoreCase))
                history.RemoveAt(index);
        }

        history.Insert(0, normalized);
        while (history.Count > MaxEntries)
            history.RemoveAt(history.Count - 1);
    }

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? history)
        => (history ?? [])
            .Select(query => query?.Trim())
            .Where(query => !string.IsNullOrWhiteSpace(query))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxEntries)
            .ToArray()!;
}
