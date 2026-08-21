using LightUpUI.Services;

namespace LightUpTest.Launcher;

public sealed class CalculatorSearchProviderTests
{
    [Theory]
    [InlineData("1 + 2 * 3", "7")]
    [InlineData("(1 + 2) * 3", "9")]
    [InlineData("10 ÷ 4", "2.5")]
    [InlineData("-5 + 2", "-3")]
    [InlineData("7 % 4", "3")]
    public async Task SearchAsync_returns_a_copy_action_for_valid_expressions(string expression, string expectedResult)
    {
        var provider = new CalculatorSearchProvider();

        var results = await provider.SearchAsync(expression, TestContext.Current.CancellationToken);

        var result = Assert.Single(results);
        Assert.Equal("action:copy-calculation", result.Id);
        Assert.Equal(expectedResult, result.Arguments);
        Assert.StartsWith(expression, result.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("LightUp docs")]
    [InlineData("1 / 0")]
    [InlineData("1 +")]
    [InlineData("(1 + 2")]
    public async Task SearchAsync_ignores_non_calculations_and_invalid_expressions(string query)
    {
        var provider = new CalculatorSearchProvider();

        var results = await provider.SearchAsync(query, TestContext.Current.CancellationToken);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Equals_prefix_routes_a_valid_expression_to_the_calculator()
    {
        var provider = new CalculatorSearchProvider();

        var result = Assert.Single(await provider.SearchAsync("= (1 + 2) * 3", TestContext.Current.CancellationToken));

        Assert.Equal("9", result.Arguments);
        Assert.StartsWith("= (1 + 2) * 3", result.Title);
    }
}
