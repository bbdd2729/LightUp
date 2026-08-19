using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class BuiltInActionSearchProvider : ISearchProvider
{
    public Task<IReadOnlyList<LauncherItem>> SearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalizedQuery = query.Trim();
        var everythingTitle = normalizedQuery.Length == 0
            ? "Everything 搜索"
            : $"Everything 搜索“{normalizedQuery}”";

        IReadOnlyList<LauncherItem> results =
        [
            new LauncherItem(
                "action:everything",
                everythingTitle,
                "使用 Everything 搜索本机文件（需安装 Everything）",
                "lightup:everything",
                normalizedQuery,
                LauncherItemKind.Action),
            new LauncherItem(
                "action:tiles",
                "打开磁贴启动器",
                "管理分类与快捷方式",
                "lightup:tiles",
                null,
                LauncherItemKind.Action),
            new LauncherItem(
                "action:settings",
                "打开 LightUp 设置",
                "搜索模式、快捷键与扩展设置",
                "lightup:settings",
                null,
                LauncherItemKind.Action)
        ];

        return Task.FromResult(results);
    }
}
