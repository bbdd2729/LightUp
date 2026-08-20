using System;

namespace LightUpUI.Services;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    bool Start();
    void Stop();
}
