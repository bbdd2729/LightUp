using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace LightUpUI.Services;

public sealed class WindowsGlobalHotkeyService : IGlobalHotkeyService
{
    private const int ModAlt = 0x0001;
    private const int ModShift = 0x0004;
    private const int VirtualKeySpace = 0x20;
    private const uint WmHotKey = 0x0312;
    private const uint WmQuit = 0x0012;
    private const uint PmNoRemove = 0x0000;
    private static int s_nextHotkeyId = 0x4C55;

    private readonly uint _modifiers;
    private readonly uint _virtualKey;
    private readonly int _hotkeyId;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly object _gate = new();
    private Thread? _thread;
    private uint _threadId;
    private bool _registered;
    private bool _disposed;

    public event EventHandler? HotkeyPressed;

    public WindowsGlobalHotkeyService(bool includeShift = false)
    {
        _modifiers = (uint)(ModAlt | (includeShift ? ModShift : 0));
        _virtualKey = VirtualKeySpace;
        _hotkeyId = Interlocked.Increment(ref s_nextHotkeyId);
    }

    public void Start()
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_thread is not null || !OperatingSystem.IsWindows())
                return;

            _ready.Reset();
            _thread = new Thread(MessageLoop)
            {
                IsBackground = true,
                Name = "LightUp.GlobalHotkey"
            };
            _thread.Start();
        }

        _ready.Wait();
    }

    public void Stop()
    {
        Thread? thread;
        lock (_gate)
        {
            thread = _thread;
            if (thread is null)
                return;

            if (_threadId != 0)
                PostThreadMessage(_threadId, WmQuit, IntPtr.Zero, IntPtr.Zero);
        }

        thread.Join(TimeSpan.FromSeconds(2));
        lock (_gate)
        {
            if (_thread == thread)
                _thread = null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Stop();
        _ready.Dispose();
    }

    private void MessageLoop()
    {
        _threadId = GetCurrentThreadId();
        _ = PeekMessage(out _, IntPtr.Zero, 0, 0, PmNoRemove);
        _registered = RegisterHotKey(IntPtr.Zero, _hotkeyId, _modifiers, _virtualKey);
        _ready.Set();

        if (!_registered)
            return;

        try
        {
            while (GetMessage(out var message, IntPtr.Zero, 0, 0) > 0)
            {
                if (message.Message == WmHotKey && message.WParam.ToInt32() == _hotkeyId)
                {
                    try
                    {
                        HotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                    catch
                    {
                        // Never terminate the native message loop because of an app callback.
                    }
                }
            }
        }
        finally
        {
            UnregisterHotKey(IntPtr.Zero, _hotkeyId);
            _registered = false;
            _threadId = 0;
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern int GetMessage(out NativeMessage message, IntPtr hWnd, uint minFilter, uint maxFilter);

    [DllImport("user32.dll")]
    private static extern bool PeekMessage(out NativeMessage message, IntPtr hWnd, uint minFilter, uint maxFilter, uint removeMessage);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostThreadMessage(uint threadId, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMessage
    {
        public IntPtr HWnd;
        public uint Message;
        public IntPtr WParam;
        public IntPtr LParam;
        public uint Time;
        public int PointX;
        public int PointY;
    }
}
