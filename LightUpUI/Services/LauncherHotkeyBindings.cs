using System;

namespace LightUpUI.Services;

public sealed class LauncherHotkeyBindings : IDisposable
{
    private readonly GlobalHotkeyBinding _searchBinding;
    private readonly GlobalHotkeyBinding _tileLauncherBinding;

    public LauncherHotkeyBindings(IGlobalHotkeyServiceFactory factory)
    {
        _searchBinding = new GlobalHotkeyBinding(factory);
        _tileLauncherBinding = new GlobalHotkeyBinding(factory);
        _searchBinding.HotkeyPressed += (_, e) => SearchHotkeyPressed?.Invoke(this, e);
        _tileLauncherBinding.HotkeyPressed += (_, e) => TileLauncherHotkeyPressed?.Invoke(this, e);
    }

    public event EventHandler? SearchHotkeyPressed;
    public event EventHandler? TileLauncherHotkeyPressed;

    public bool TryApply(string? searchHotkeyText, string? tileLauncherHotkeyText, out string? error)
    {
        if (!GlobalHotkeyParser.TryParse(searchHotkeyText, out var searchGesture, out error))
            return false;
        if (!GlobalHotkeyParser.TryParse(tileLauncherHotkeyText, out var tileLauncherGesture, out error))
            return false;
        if (searchGesture == tileLauncherGesture)
        {
            error = "搜索栏和磁贴启动器不能使用相同的全局快捷键。";
            return false;
        }

        var previousSearchGesture = _searchBinding.Gesture;
        var previousTileLauncherGesture = _tileLauncherBinding.Gesture;
        if (previousSearchGesture == tileLauncherGesture && previousTileLauncherGesture == searchGesture)
        {
            error = "两个已注册的快捷键不能直接互换，请先为其中一个设置临时快捷键。";
            return false;
        }

        var searchChanged = previousSearchGesture != searchGesture;
        if (searchChanged && !_searchBinding.TryApply(searchGesture, out error))
            return false;

        var tileLauncherChanged = previousTileLauncherGesture != tileLauncherGesture;
        if (!tileLauncherChanged || _tileLauncherBinding.TryApply(tileLauncherGesture, out error))
            return true;

        if (searchChanged)
        {
            if (previousSearchGesture is { } previousGesture)
                _ = _searchBinding.TryApply(previousGesture, out _);
            else
                _searchBinding.Dispose();
        }

        return false;
    }

    public void Dispose()
    {
        _searchBinding.Dispose();
        _tileLauncherBinding.Dispose();
    }
}
