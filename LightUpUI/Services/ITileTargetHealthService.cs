using LightUpUI.Models.Tiles;

namespace LightUpUI.Services;

public interface ITileTargetHealthService
{
    TileTargetHealthResult Evaluate(TileItem item);
}
