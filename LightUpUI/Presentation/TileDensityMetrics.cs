using LightUpUI.Models.Tiles;

namespace LightUpUI.Presentation;

public readonly record struct TileDensityMetrics(
    double TileWidth,
    double TileHeight,
    double IconBoxSize,
    double CardPadding,
    double CardRowSpacing);
