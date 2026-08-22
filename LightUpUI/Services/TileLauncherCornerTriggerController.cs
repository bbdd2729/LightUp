using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;
using LightUpUI.Models;
using LightUpUI.Presentation;

namespace LightUpUI.Services;

public sealed class TileLauncherCornerTriggerController : IDisposable
{
    public const int HotZoneSize = 8;

    private readonly ICursorPositionService _cursorPositionService;
    private readonly Func<IReadOnlyList<TileLauncherScreenArea>> _screenAreasProvider;
    private readonly ITileLauncherCornerHost _host;
    private readonly DispatcherTimer _timer;
    private SearchLauncherSettings _settings;
    private ScreenCorner? _currentCorner;
    private PixelRect? _currentWorkingArea;
    private DateTimeOffset? _enteredAt;
    private bool _hasTriggered;
    private bool _pointerEnteredWindow;
    private bool _disposed;

    public TileLauncherCornerTriggerController(
        ICursorPositionService cursorPositionService,
        Func<IReadOnlyList<TileLauncherScreenArea>> screenAreasProvider,
        ITileLauncherCornerHost host,
        SearchLauncherSettings settings,
        TimeSpan? pollInterval = null)
    {
        _cursorPositionService = cursorPositionService;
        _screenAreasProvider = screenAreasProvider;
        _host = host;
        _settings = SearchLauncherSettingsPolicy.Normalize(settings);
        _timer = new DispatcherTimer
        {
            Interval = pollInterval ?? TimeSpan.FromMilliseconds(75)
        };
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        if (!_disposed)
            _timer.Start();
    }

    public void ApplySettings(SearchLauncherSettings settings)
    {
        _settings = SearchLauncherSettingsPolicy.Normalize(settings);
        if (!_settings.EnableTileCornerTrigger)
            ResetCornerState();
    }

    public void Poll(DateTimeOffset now)
    {
        if (_disposed || !_settings.EnableTileCornerTrigger)
            return;

        var screenAreas = _screenAreasProvider();
        var cursor = _cursorPositionService.GetPosition();
        var corner = TileCornerTriggerPolicy.GetCorner(cursor, screenAreas, HotZoneSize);

        if (corner is null)
        {
            if (!(_hasTriggered && _host.IsVisible && _host.IsCornerActivated))
                ResetCornerState();
            else
            {
                _currentCorner = null;
                _currentWorkingArea = null;
                _enteredAt = null;
            }
        }
        else if (_currentCorner != corner)
        {
            _currentCorner = corner;
            _currentWorkingArea = FindWorkingArea(cursor, screenAreas, corner.Value);
            _enteredAt = now;
            if (!(_host.IsVisible && _host.IsCornerActivated))
                _hasTriggered = false;
        }

        if (corner is not null && !_hasTriggered && !_host.IsVisible && _enteredAt is not null
            && TileCornerTriggerPolicy.HasDwelled(
                _enteredAt.Value,
                now,
                TimeSpan.FromMilliseconds(_settings.TileCornerTriggerDelayMilliseconds)))
        {
            if (_currentWorkingArea is { } workingArea)
            {
                _host.ShowFromCorner(corner.Value, workingArea);
                _hasTriggered = true;
            }
        }

        if (_settings.CloseTileLauncherOnPointerLeave
            && _hasTriggered
            && _host.IsVisible
            && _host.IsCornerActivated
            && _host.WindowBounds is { } windowBounds)
        {
            if (TileCornerTriggerPolicy.IsPointerInsideWindow(cursor, windowBounds))
                _pointerEnteredWindow = true;
            else if (_pointerEnteredWindow)
            {
                _host.Hide();
                ResetCornerState();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
        ResetCornerState();
    }

    private void Timer_Tick(object? sender, EventArgs e) => Poll(DateTimeOffset.UtcNow);

    private static PixelRect? FindWorkingArea(
        PixelPoint cursor,
        IReadOnlyList<TileLauncherScreenArea> screenAreas,
        ScreenCorner corner)
    {
        foreach (var screenArea in screenAreas)
        {
            if (TileCornerTriggerPolicy.IsInCorner(cursor, screenArea.Bounds, corner, HotZoneSize))
                return screenArea.WorkingArea;
        }

        return null;
    }

    private void ResetCornerState()
    {
        _currentCorner = null;
        _currentWorkingArea = null;
        _enteredAt = null;
        _hasTriggered = false;
        _pointerEnteredWindow = false;
    }
}
