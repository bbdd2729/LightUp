using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class CalculatorSearchProvider : ISearchProvider
{
    public Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var originalQuery = query.Trim();
        var expression = originalQuery.StartsWith('=')
            ? originalQuery[1..].Trim()
            : originalQuery;
        if (!CalculatorExpression.TryEvaluate(expression, out var result))
            return Task.FromResult<IReadOnlyList<LauncherItem>>([]);

        var formattedResult = CalculatorExpression.Format(result);
        var titleExpression = originalQuery.StartsWith('=') ? originalQuery : expression;
        IReadOnlyList<LauncherItem> results =
        [
            new LauncherItem(
                "action:copy-calculation",
                $"{titleExpression} = {formattedResult}",
                "按 Enter 复制结果到剪贴板",
                "lightup:calculator",
                formattedResult,
                LauncherItemKind.Action,
                Relevance: 700)
        ];
        return Task.FromResult(results);
    }
}
