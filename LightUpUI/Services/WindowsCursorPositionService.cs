using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace LightUpUI.Services;

public sealed class WindowsCursorPositionService : ICursorPositionService
{
    public PixelPoint GetPosition()
    {
        if (!OperatingSystem.IsWindows() || !GetCursorPos(out var point))
            return default;

        return new PixelPoint(point.X, point.Y);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
