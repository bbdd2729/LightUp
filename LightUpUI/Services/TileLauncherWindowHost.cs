using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LightUpUI.Presentation;
using LightUpUI.ViewModels;
using LightUpUI.Views;

namespace LightUpUI.Services;

public sealed class TileLauncherWindowHost : ITileLauncherWindowHost, ITileLauncherCornerHost
{
    private readonly TileLauncherViewModel _viewModel;
    private TileLauncherWindow? _window;
    private Task? _loadTask;

    public bool CloseAfterLaunch { get; set; } = true;
    public Action<PixelPoint>? CornerPositionApplier { get; set; }
    public bool IsCornerActivated { get; private set; }
    public PixelRect? WindowBounds
    {
        get
        {
            if (_window is not { IsVisible: true } window || window.FrameSize is not { } frameSize
                || frameSize.Width <= 0 || frameSize.Height <= 0)
                return null;

            return new PixelRect(window.Position, GetPixelSize(window, frameSize));
        }
    }

    public TileLauncherWindowHost(TileLauncherViewModel viewModel)
    {
        _viewModel = viewModel;
        _viewModel.LaunchSucceeded += ViewModel_LaunchSucceeded;
    }

    public bool IsVisible => _window?.IsVisible == true;

    public void Attach(TileLauncherWindow window) => _window = window;

    public Task EnsureLoadedAsync() => _loadTask ??= _viewModel.LoadAsync();

    public void Toggle()
    {
        if (IsVisible)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        IsCornerActivated = false;
        ShowCore(null, null);
    }

    public void ShowFromCorner(ScreenCorner corner, PixelRect workingArea)
    {
        IsCornerActivated = true;
        ShowCore(corner, workingArea);
    }

    private void ShowCore(ScreenCorner? corner, PixelRect? workingArea)
    {
        if (_window is null)
            return;

        _ = EnsureLoadedAsync();

        if (corner is not null)
            _window.WindowStartupLocation = WindowStartupLocation.Manual;

        _window.Show();
        _window.Activate();
        if (corner is { } screenCorner && workingArea is { } area)
        {
            ApplyCornerPosition(screenCorner, area);
            Dispatcher.UIThread.Post(() => ApplyCornerPosition(screenCorner, area), DispatcherPriority.Render);
            Dispatcher.UIThread.Post(() => ApplyCornerPosition(screenCorner, area), DispatcherPriority.ApplicationIdle);
        }
        Dispatcher.UIThread.Post(_window.FocusSearchBox);
    }

    private void ApplyCornerPosition(ScreenCorner corner, PixelRect workingArea)
    {
        if (_window is not { IsVisible: true } window)
            return;

        var size = window.FrameSize is { } frameSize && frameSize.Width > 0 && frameSize.Height > 0
            ? GetPixelSize(window, frameSize)
            : GetPixelSize(window, new Size(window.Width, window.Height));
        var position = TileCornerTriggerPolicy.GetWindowPosition(workingArea, size, corner, 0);
        if (CornerPositionApplier is { } applyPosition)
            applyPosition(position);
        else
            window.Position = position;
    }

    private static PixelSize GetPixelSize(Window window, Size logicalSize)
    {
        var scaling = window.DesktopScaling;
        if (scaling <= 0)
            scaling = 1;

        return PixelSize.FromSize(logicalSize, scaling);
    }

    public void Hide()
    {
        IsCornerActivated = false;
        _window?.Hide();
    }

    private void ViewModel_LaunchSucceeded(object? sender, EventArgs e)
    {
        if (CloseAfterLaunch)
            Hide();
    }

}
