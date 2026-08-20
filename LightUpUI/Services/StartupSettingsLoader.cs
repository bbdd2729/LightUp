using System;
using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public static class StartupSettingsLoader
{
    public static async Task<SearchLauncherSettings> LoadAsync(
        ISearchLauncherSettingsStore settingsStore,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(true);
            return SearchLauncherSettingsPolicy.Normalize(settings);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return SearchLauncherSettingsPolicy.Normalize(null);
        }
        catch
        {
            return SearchLauncherSettingsPolicy.Normalize(null);
        }
    }
}
