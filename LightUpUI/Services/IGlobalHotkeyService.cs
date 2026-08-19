using System;

namespace LightUpUI.Services;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? HotkeyPressed;
    void Start();
    void Stop();
}
