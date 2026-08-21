using LightUpUI.Models.Tiles;
using LightUpUI.Presentation;

namespace LightUpTest.Windowing;

public sealed class TileDensityPolicyTests
{
    [Theory]
    [InlineData(TileDensity.Compact, 156, 132, 44, 12, 7)]
    [InlineData(TileDensity.Comfortable, 188, 160, 56, 14, 9)]
    public void GetMetrics_returns_the_design_tokens_for_each_density(
        TileDensity density,
        double tileWidth,
        double tileHeight,
        double iconSize,
        double padding,
        double rowSpacing)
    {
        var metrics = TileLauncherLayoutPolicy.GetDensityMetrics(density);

        Assert.Equal(tileWidth, metrics.TileWidth);
        Assert.Equal(tileHeight, metrics.TileHeight);
        Assert.Equal(iconSize, metrics.IconBoxSize);
        Assert.Equal(padding, metrics.CardPadding);
        Assert.Equal(rowSpacing, metrics.CardRowSpacing);
    }

    [Fact]
    public void Normalize_falls_back_to_compact_for_unknown_values()
    {
        Assert.Equal(TileDensity.Compact, TileDensityPolicy.Normalize((TileDensity)42));
    }

    [Fact]
    public void Tile_launcher_window_declares_both_density_layout_variants()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("ListBox.tile-density-comfortable ListBoxItem", markup);
        Assert.Contains("x:Name=\"TileWrapPanel\"", markup);
    }
}
