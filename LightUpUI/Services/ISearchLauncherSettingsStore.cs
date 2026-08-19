using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models;

namespace LightUpUI.Services;

public interface ISearchLauncherSettingsStore
{
    Task<SearchLauncherSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(SearchLauncherSettings settings, CancellationToken cancellationToken);
}
