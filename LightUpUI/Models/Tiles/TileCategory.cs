using System.Collections.Generic;

namespace LightUpUI.Models.Tiles;

public sealed class TileCategory
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<TileItem> Items { get; set; } = [];
}
