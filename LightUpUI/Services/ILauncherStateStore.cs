using System.Threading;
using System.Threading.Tasks;
using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public interface ILauncherStateStore
{
    Task<TileLauncherState> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(TileLauncherState state, CancellationToken cancellationToken);
}
