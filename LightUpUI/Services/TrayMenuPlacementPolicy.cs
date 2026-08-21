using System;
using Avalonia;

namespace LightUpUI.Services;

public static class TrayMenuPlacementPolicy
{
    public static PixelPoint GetPosition(PixelRect workingArea, PixelSize menuSize, int margin = 16)
    {
        return new PixelPoint(
            GetAxisPosition(workingArea.X, workingArea.Right, menuSize.Width, margin),
            GetAxisPosition(workingArea.Y, workingArea.Bottom, menuSize.Height, margin));
    }

    private static int GetAxisPosition(int areaStart, int areaEnd, int contentSize, int margin)
    {
        if (contentSize >= areaEnd - areaStart)
            return areaStart;

        var preferred = areaEnd - contentSize - margin;
        var minimum = areaStart + margin;
        var maximum = areaEnd - contentSize;
        return Math.Clamp(preferred, minimum, maximum);
    }
}
