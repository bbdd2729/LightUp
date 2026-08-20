using LightUpUI.Models;

namespace LightUpTest.Launcher;

public sealed class SearchResultLimitPolicyTests
{
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(30, 30)]
    [InlineData(101, 100)]
    public void Normalize_clamps_the_configured_result_limit(int configuredLimit, int expectedLimit)
    {
        Assert.Equal(expectedLimit, SearchResultLimitPolicy.Normalize(configuredLimit));
    }

    [Fact]
    public void GetVisibleResultLimit_caps_empty_queries_without_ignoring_a_smaller_user_limit()
    {
        Assert.Equal(10, SearchResultLimitPolicy.GetVisibleResultLimit(30, isEmptyQuery: true));
        Assert.Equal(3, SearchResultLimitPolicy.GetVisibleResultLimit(3, isEmptyQuery: true));
        Assert.Equal(30, SearchResultLimitPolicy.GetVisibleResultLimit(30, isEmptyQuery: false));
    }
}
