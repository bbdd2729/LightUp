using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class WeightedSearchProvider : ISearchProvider
{
    public const int MaximumAbsoluteWeight = 1000;

    private readonly ISearchProvider _inner;
    private readonly int _weight;

    public WeightedSearchProvider(ISearchProvider inner, int weight)
    {
        _inner = inner;
        _weight = NormalizeWeight(weight);
    }

    public async Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var results = await _inner.SearchAsync(query, cancellationToken);
        return results
            .Select(item => item with
            {
                Relevance = (int)System.Math.Clamp(
                    (long)item.Relevance + _weight,
                    int.MinValue,
                    int.MaxValue)
            })
            .ToArray();
    }

    public static int NormalizeWeight(int weight)
        => System.Math.Clamp(weight, -MaximumAbsoluteWeight, MaximumAbsoluteWeight);
}
