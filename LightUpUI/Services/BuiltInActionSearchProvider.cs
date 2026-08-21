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
        if (TryGetPrefixedQuery(normalizedQuery, '!', out var everythingQuery))
            return Task.FromResult<IReadOnlyList<LauncherItem>>([CreateEverythingAction(normalizedQuery, everythingQuery)]);
        if (TryGetPrefixedQuery(normalizedQuery, '?', out var webQuery))
        {
            return Task.FromResult<IReadOnlyList<LauncherItem>>(
            [
                new LauncherItem(
                    "action:web-search",
                    $"使用 Bing 搜索“{normalizedQuery}”",
                    "使用默认浏览器进行网页搜索",
                    "lightup:web-search",
                    webQuery,
                    LauncherItemKind.Action)
            ]);
        }
        if (normalizedQuery.StartsWith('='))
            return Task.FromResult<IReadOnlyList<LauncherItem>>([]);

        var everythingTitle = normalizedQuery.Length == 0
            ? "Everything 搜索"
            : $"Everything 搜索“{normalizedQuery}”";

        List<LauncherItem> results =
        [
            CreateEverythingAction(everythingTitle, normalizedQuery),
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
                LauncherItemKind.Action),
            new LauncherItem(
                "action:windows-settings",
                "打开 Windows 设置",
                "打开 Windows 系统设置 / Windows Settings",
                "lightup:windows-settings",
                null,
                LauncherItemKind.Action),
            new LauncherItem(
                "action:control-panel",
                "打开控制面板",
                "管理传统 Windows 控制面板 / Control Panel",
                "lightup:control-panel",
                null,
                LauncherItemKind.Action),
            new LauncherItem(
                "action:file-explorer",
                "打开文件资源管理器",
                "浏览此电脑中的文件 / File Explorer",
                "lightup:file-explorer",
                null,
                LauncherItemKind.Action)
        ];

        if (TryGetHttpUri(normalizedQuery, out var uri))
        {
            results.Add(new LauncherItem(
                "action:open-url",
                $"打开“{uri}”",
                "使用默认浏览器打开链接",
                "lightup:open-url",
                uri,
                LauncherItemKind.Action));
        }
        else if (normalizedQuery.Length > 0)
        {
            results.Add(new LauncherItem(
                "action:web-search",
                $"使用 Bing 搜索“{normalizedQuery}”",
                "使用默认浏览器进行网页搜索",
                "lightup:web-search",
                normalizedQuery,
                LauncherItemKind.Action));
        }

        return Task.FromResult<IReadOnlyList<LauncherItem>>(results);
    }

    private static LauncherItem CreateEverythingAction(string title, string query) => new(
        "action:everything",
        title,
        "使用 Everything 搜索本机文件（需安装 Everything）",
        "lightup:everything",
        query,
        LauncherItemKind.Action);

    private static bool TryGetPrefixedQuery(string query, char prefix, out string value)
    {
        value = string.Empty;
        if (!query.StartsWith(prefix))
            return false;

        value = query[1..].Trim();
        return value.Length > 0;
    }

    private static bool TryGetHttpUri(string value, out string uri)
    {
        uri = string.Empty;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            || !parsed.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && !parsed.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(parsed.Host))
        {
            return false;
        }

        uri = parsed.AbsoluteUri;
        return true;
    }
}
