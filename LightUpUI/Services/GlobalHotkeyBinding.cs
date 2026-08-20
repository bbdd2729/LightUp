using System;

namespace LightUpUI.Services;

public interface IGlobalHotkeyServiceFactory
{
    IGlobalHotkeyService Create(GlobalHotkeyGesture gesture);
}

public sealed class WindowsGlobalHotkeyServiceFactory : IGlobalHotkeyServiceFactory
{
    public IGlobalHotkeyService Create(GlobalHotkeyGesture gesture) => new WindowsGlobalHotkeyService(gesture);
}

public sealed class GlobalHotkeyBinding(IGlobalHotkeyServiceFactory factory) : IDisposable
{
    private IGlobalHotkeyService? _activeService;

    public event EventHandler? HotkeyPressed;
    public GlobalHotkeyGesture? Gesture { get; private set; }

    public bool TryApply(GlobalHotkeyGesture gesture, out string? error)
    {
        if (Gesture == gesture)
        {
            error = null;
            return true;
        }

        IGlobalHotkeyService? replacement = null;
        try
        {
            replacement = factory.Create(gesture);
            replacement.HotkeyPressed += OnHotkeyPressed;
            if (!replacement.Start())
            {
                error = $"无法注册全局快捷键 {gesture.ToConfigText()}，它可能已被其他程序占用。";
                return false;
            }

            var previous = _activeService;
            _activeService = replacement;
            Gesture = gesture;
            replacement = null;

            if (previous is not null)
            {
                previous.HotkeyPressed -= OnHotkeyPressed;
                previous.Dispose();
            }

            error = null;
            return true;
        }
        catch (Exception)
        {
            error = $"无法注册全局快捷键 {gesture.ToConfigText()}。";
            return false;
        }
        finally
        {
            if (replacement is not null)
            {
                replacement.HotkeyPressed -= OnHotkeyPressed;
                replacement.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (_activeService is null)
            return;

        _activeService.HotkeyPressed -= OnHotkeyPressed;
        _activeService.Dispose();
        _activeService = null;
        Gesture = null;
    }

    private void OnHotkeyPressed(object? sender, EventArgs e) => HotkeyPressed?.Invoke(this, e);
}
