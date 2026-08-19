using System.Collections.Generic;

namespace LightUpUI.Models.Tiles;

public sealed class TileLauncherState
{
    public string SelectedCategoryId { get; set; } = "all";
    public List<TileCategory> Categories { get; set; } = [];
}
