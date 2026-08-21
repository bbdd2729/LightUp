namespace LightUpUI.Models.Tiles;

public enum TileDensity
{
    Compact,
    Comfortable
}

public static class TileDensityPolicy
{
    public static TileDensity Normalize(TileDensity density)
        => density == TileDensity.Comfortable ? TileDensity.Comfortable : TileDensity.Compact;
}
