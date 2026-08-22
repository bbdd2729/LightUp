using System;

namespace LightUpUI.Models;

public static class TileCornerTriggerSettingsPolicy
{
    public const int DefaultDelayMilliseconds = 700;
    public const int MinimumDelayMilliseconds = 200;
    public const int MaximumDelayMilliseconds = 5000;

    public static int NormalizeDelay(int value)
        => Math.Clamp(value <= 0 ? DefaultDelayMilliseconds : value,
            MinimumDelayMilliseconds,
            MaximumDelayMilliseconds);
}
