using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class SearchHistoryService : ISearchHistoryService
{
    private readonly ISearchLauncherSettingsStore _settingsStore;
    private readonly SearchLauncherSettings _settings;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SearchHistoryService(
        ISearchLauncherSettingsStore settingsStore,
        SearchLauncherSettings settings)
    {
        _settingsStore = settingsStore;
        _settings = settings;
    }

    public IReadOnlyList<string> RecentQueries => _settings.SaveQueryHistory
        ? _settings.QueryHistory
        : [];

    public async Task RecordAsync(string query, CancellationToken cancellationToken)
    {
        if (!_settings.SaveQueryHistory || string.IsNullOrWhiteSpace(query))
            return;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SearchHistoryPolicy.Record(_settings.QueryHistory, query);
            await _settingsStore.SaveAsync(_settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _settings.QueryHistory.Clear();
            await _settingsStore.SaveAsync(_settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
