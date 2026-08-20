using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using LightUpUI.Models;

namespace LightUpUI.Services;

public sealed class LauncherWindowStateTracker : IDisposable
{
    private static readonly PixelRect FallbackWorkingArea = new(0, 0, 1920, 1080);
    private readonly Window _window;
    private readonly Action<LauncherWindowState> _updateState;
    private readonly Func<Task> _persistState;
    private readonly Size _minimumSize;
    private readonly Size _maximumSize;
    private CancellationTokenSource? _pendingSave;
    private bool _isRestoring;
    private bool _disposed;

    public LauncherWindowStateTracker(
        Window window,
        Action<LauncherWindowState> updateState,
        Func<Task> persistState,
        Size minimumSize,
        Size maximumSize)
    {
        _window = window;
        _updateState = updateState;
        _persistState = persistState;
        _minimumSize = minimumSize;
        _maximumSize = maximumSize;
        _window.PositionChanged += Window_PositionChanged;
        _window.SizeChanged += Window_SizeChanged;
        _window.PropertyChanged += Window_PropertyChanged;
    }

    public void Restore(LauncherWindowState? savedState)
    {
        if (_disposed)
            return;

        var normalized = LauncherWindowStatePolicy.Normalize(
            savedState,
            GetWorkingArea(),
            _minimumSize,
            _maximumSize);
        _isRestoring = true;
        try
        {
            _window.Width = normalized.Width;
            _window.Height = normalized.Height;
            if (normalized.HasPosition)
                _window.Position = new PixelPoint(normalized.X, normalized.Y);
            _window.Topmost = normalized.IsTopmost;
        }
        finally
        {
            _isRestoring = false;
        }
    }

    public LauncherWindowState Capture() => new()
    {
        HasPosition = true,
        X = _window.Position.X,
        Y = _window.Position.Y,
        Width = Math.Max(0, (int)Math.Round(_window.Width)),
        Height = Math.Max(0, (int)Math.Round(_window.Height)),
        IsTopmost = _window.Topmost
    };

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _window.PositionChanged -= Window_PositionChanged;
        _window.SizeChanged -= Window_SizeChanged;
        _window.PropertyChanged -= Window_PropertyChanged;
        _pendingSave?.Cancel();
        _pendingSave?.Dispose();
    }

    public async Task FlushAsync()
    {
        if (_disposed)
            return;

        _pendingSave?.Cancel();
        _pendingSave?.Dispose();
        _pendingSave = null;
        _updateState(Capture());
        try
        {
            await _persistState().ConfigureAwait(false);
        }
        catch
        {
            // A final state write must not block application shutdown.
        }
    }

    private void Window_PositionChanged(object? sender, PixelPointEventArgs e) => QueueSave();

    private void Window_SizeChanged(object? sender, SizeChangedEventArgs e) => QueueSave();

    private void Window_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.TopmostProperty)
            QueueSave();
    }

    private void QueueSave()
    {
        if (_disposed || _isRestoring)
            return;

        _updateState(Capture());
        _pendingSave?.Cancel();
        _pendingSave?.Dispose();
        var cancellation = new CancellationTokenSource();
        _pendingSave = cancellation;
        _ = PersistAfterDelayAsync(cancellation);
    }

    private async Task PersistAfterDelayAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellation.Token).ConfigureAwait(false);
            await _persistState().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // A window-state write must not terminate the UI event loop.
        }
    }

    private PixelRect GetWorkingArea()
    {
        try
        {
            return _window.Screens.ScreenFromWindow(_window)?.WorkingArea
                ?? _window.Screens.Primary?.WorkingArea
                ?? FallbackWorkingArea;
        }
        catch
        {
            return FallbackWorkingArea;
        }
    }
}
