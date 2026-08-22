using System;
using System.Collections.Generic;
using Avalonia;

namespace LightUpUI.Presentation;

public enum ScreenCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public readonly record struct TileLauncherScreenArea(PixelRect Bounds, PixelRect WorkingArea);

public static class TileCornerTriggerPolicy
{
    public static ScreenCorner? GetCorner(
        PixelPoint cursor,
        IReadOnlyList<PixelRect> workingAreas,
        int hotZoneSize)
    {
        if (hotZoneSize <= 0)
            return null;

        foreach (var workingArea in workingAreas)
        {
            if (IsInCorner(cursor, workingArea, ScreenCorner.TopLeft, hotZoneSize))
                return ScreenCorner.TopLeft;
            if (IsInCorner(cursor, workingArea, ScreenCorner.TopRight, hotZoneSize))
                return ScreenCorner.TopRight;
            if (IsInCorner(cursor, workingArea, ScreenCorner.BottomLeft, hotZoneSize))
                return ScreenCorner.BottomLeft;
            if (IsInCorner(cursor, workingArea, ScreenCorner.BottomRight, hotZoneSize))
                return ScreenCorner.BottomRight;
        }

        return null;
    }

    public static ScreenCorner? GetCorner(
        PixelPoint cursor,
        IReadOnlyList<TileLauncherScreenArea> screenAreas,
        int hotZoneSize)
    {
        if (hotZoneSize <= 0)
            return null;

        foreach (var screenArea in screenAreas)
        {
            if (IsInCorner(cursor, screenArea.Bounds, ScreenCorner.TopLeft, hotZoneSize))
                return ScreenCorner.TopLeft;
            if (IsInCorner(cursor, screenArea.Bounds, ScreenCorner.TopRight, hotZoneSize))
                return ScreenCorner.TopRight;
            if (IsInCorner(cursor, screenArea.Bounds, ScreenCorner.BottomLeft, hotZoneSize))
                return ScreenCorner.BottomLeft;
            if (IsInCorner(cursor, screenArea.Bounds, ScreenCorner.BottomRight, hotZoneSize))
                return ScreenCorner.BottomRight;
        }

        return null;
    }

    public static bool IsInCorner(
        PixelPoint cursor,
        PixelRect workingArea,
        ScreenCorner corner,
        int hotZoneSize)
    {
        if (hotZoneSize <= 0 || workingArea.Width < hotZoneSize || workingArea.Height < hotZoneSize)
            return false;

        var x = corner is ScreenCorner.TopLeft or ScreenCorner.BottomLeft
            ? cursor.X >= workingArea.X && cursor.X < workingArea.X + hotZoneSize
            : cursor.X < workingArea.Right && cursor.X >= workingArea.Right - hotZoneSize;
        var y = corner is ScreenCorner.TopLeft or ScreenCorner.TopRight
            ? cursor.Y >= workingArea.Y && cursor.Y < workingArea.Y + hotZoneSize
            : cursor.Y < workingArea.Bottom && cursor.Y >= workingArea.Bottom - hotZoneSize;
        return x && y;
    }

    public static bool HasDwelled(DateTimeOffset enteredAt, DateTimeOffset now, TimeSpan delay)
        => now >= enteredAt && now - enteredAt >= delay;

    public static bool IsPointerInsideWindow(PixelPoint cursor, PixelRect windowBounds)
        => cursor.X >= windowBounds.X
            && cursor.X < windowBounds.Right
            && cursor.Y >= windowBounds.Y
            && cursor.Y < windowBounds.Bottom;

    public static PixelPoint GetWindowPosition(
        PixelRect workingArea,
        PixelSize windowSize,
        ScreenCorner corner,
        int margin = 12)
    {
        var safeMargin = Math.Max(0, margin);
        var width = Math.Min(Math.Max(0, windowSize.Width), Math.Max(0, workingArea.Width - safeMargin * 2));
        var height = Math.Min(Math.Max(0, windowSize.Height), Math.Max(0, workingArea.Height - safeMargin * 2));
        var left = corner is ScreenCorner.TopLeft or ScreenCorner.BottomLeft
            ? workingArea.X + safeMargin
            : workingArea.Right - width - safeMargin;
        var top = corner is ScreenCorner.TopLeft or ScreenCorner.TopRight
            ? workingArea.Y + safeMargin
            : workingArea.Bottom - height - safeMargin;
        return new PixelPoint(left, top);
    }
}
