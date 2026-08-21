using LightUpUI.Models;

namespace LightUpTest.Settings;

public sealed class SearchHistoryPolicyTests
{
    [Fact]
    public void Normalize_removes_empty_duplicate_and_excess_entries()
    {
        var input = Enumerable.Range(0, 25)
            .Select(index => $" query-{index} ")
            .Append("QUERY-0")
            .Append(" ");

        var normalized = SearchHistoryPolicy.Normalize(input);

        Assert.Equal(SearchHistoryPolicy.MaxEntries, normalized.Count);
        Assert.Equal("query-19", normalized[^1]);
        Assert.DoesNotContain(normalized, string.IsNullOrWhiteSpace);
    }

    [Fact]
    public void Record_moves_an_existing_query_to_the_front()
    {
        var history = new List<string> { "one", "two", "three" };

        SearchHistoryPolicy.Record(history, " TWO ");

        Assert.Equal(["TWO", "one", "three"], history);
    }
}
