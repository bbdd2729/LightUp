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
        Assert.Contains("ListBox.tile-density-comfortable Border.tile-card", markup);
        Assert.Contains("ListBox.tile-density-comfortable Border.tile-icon-box", markup);
        Assert.Contains("x:Name=\"TileWrapPanel\"", markup);
    }

    [Fact]
    public void Category_navigation_does_not_inherit_tile_density_styles()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));
        var tileListIndex = markup.IndexOf("x:Name=\"TileList\"", StringComparison.Ordinal);

        Assert.True(tileListIndex > 0);
        Assert.DoesNotContain("tile-density-comfortable", markup[..tileListIndex]);
    }

    [Fact]
    public void Tile_editor_uses_shared_field_style_without_fixed_short_heights()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "LightUpUI", "Views", "TileLauncherWindow.axaml");
        var markup = File.ReadAllText(Path.GetFullPath(path));

        Assert.Contains("x:Name=\"TileTitleBox\"", markup);
        Assert.Contains("x:Name=\"TileNotesBox\"", markup);
        Assert.Contains("x:Name=\"MoveCategoryBox\"", markup);
        Assert.DoesNotContain("Height=\"27\"", markup);
        Assert.DoesNotContain("Height=\"28\"", markup);
    }
}
