using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace LightUpUI.Services;

public sealed class WindowsFileIconService : IFileIconService
{
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private const uint DiNormal = 0x00000003;
    private const uint DibRgb = 0;
    private readonly FileIconCache _cache;

    public WindowsFileIconService()
    {
        _cache = new FileIconCache(LoadShellIcon);
    }

    public WindowsFileIconService(Func<string, int, IImage?> loadIcon)
    {
        _cache = new FileIconCache(loadIcon);
    }

    public IImage? GetIcon(string? preferredPath, string? fallbackPath, int size)
    {
        if (!string.IsNullOrWhiteSpace(preferredPath))
        {
            var preferredIcon = _cache.GetOrLoad(preferredPath, size);
            if (preferredIcon is not null)
                return preferredIcon;
        }

        if (!string.IsNullOrWhiteSpace(fallbackPath) &&
            !string.Equals(preferredPath, fallbackPath, StringComparison.OrdinalIgnoreCase))
        {
            return _cache.GetOrLoad(fallbackPath, size);
        }

        return null;
    }

    private static IImage? LoadShellIcon(string path, int size)
    {
        if (!OperatingSystem.IsWindows() || (!File.Exists(path) && !Directory.Exists(path)))
            return null;

        try
        {
            var fileInfo = new Shfileinfo();
            var result = SHGetFileInfo(
                path,
                0,
                ref fileInfo,
                (uint)Marshal.SizeOf<Shfileinfo>(),
                ShgfiIcon | ShgfiLargeIcon);
            if (result == IntPtr.Zero || fileInfo.Icon == IntPtr.Zero)
                return null;

            try
            {
                return CopyIconToBitmap(fileInfo.Icon, Math.Clamp(size, 16, 256));
            }
            finally
            {
                DestroyIcon(fileInfo.Icon);
            }
        }
        catch (Exception) when (OperatingSystem.IsWindows())
        {
            return null;
        }
    }

    private static IImage? CopyIconToBitmap(IntPtr icon, int size)
    {
        var deviceContext = CreateCompatibleDC(IntPtr.Zero);
        if (deviceContext == IntPtr.Zero)
            return null;

        var bitmapInfo = new Bitmapinfo
        {
            Header = new Bitmapinfoheader
            {
                Size = (uint)Marshal.SizeOf<Bitmapinfoheader>(),
                Width = size,
                Height = -size,
                Planes = 1,
                BitCount = 32,
                Compression = DibRgb
            }
        };

        var pixels = IntPtr.Zero;
        var deviceBitmap = CreateDIBSection(
            deviceContext,
            ref bitmapInfo,
            0,
            out pixels,
            IntPtr.Zero,
            0);
        if (deviceBitmap == IntPtr.Zero || pixels == IntPtr.Zero)
        {
            DeleteDC(deviceContext);
            return null;
        }

        var previousBitmap = SelectObject(deviceContext, deviceBitmap);
        try
        {
            if (!DrawIconEx(deviceContext, 0, 0, icon, size, size, 0, IntPtr.Zero, DiNormal))
                return null;

            var pixelBytes = new byte[size * size * 4];
            Marshal.Copy(pixels, pixelBytes, 0, pixelBytes.Length);
            var bitmap = new WriteableBitmap(
                new PixelSize(size, size),
                new Vector(96, 96),
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
            using (var framebuffer = bitmap.Lock())
                Marshal.Copy(pixelBytes, 0, framebuffer.Address, pixelBytes.Length);
            return bitmap;
        }
        finally
        {
            SelectObject(deviceContext, previousBitmap);
            DeleteObject(deviceBitmap);
            DeleteDC(deviceContext);
        }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string path,
        uint fileAttributes,
        ref Shfileinfo fileInfo,
        uint fileInfoSize,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DrawIconEx(
        IntPtr deviceContext,
        int x,
        int y,
        IntPtr icon,
        int width,
        int height,
        uint frame,
        IntPtr brush,
        uint flags);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateDIBSection(
        IntPtr deviceContext,
        ref Bitmapinfo bitmapInfo,
        uint usage,
        out IntPtr bits,
        IntPtr section,
        uint offset);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicsObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr graphicsObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr deviceContext);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Shfileinfo
    {
        public IntPtr Icon;
        public int IconIndex;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string DisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string TypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Bitmapinfo
    {
        public Bitmapinfoheader Header;
        public Rgbquad Colors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Bitmapinfoheader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint ImageSize;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rgbquad
    {
        public byte Blue;
        public byte Green;
        public byte Red;
        public byte Reserved;
    }
}
