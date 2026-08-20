using LightUpUI.Services;

namespace LightUpTest.Settings;

public sealed class GlobalHotkeyBindingTests
{
    [Fact]
    public void TryApply_starts_the_replacement_before_disposing_the_active_binding()
    {
        var events = new List<string>();
        var first = new FakeHotkeyService("first", startSucceeds: true, events);
        var second = new FakeHotkeyService("second", startSucceeds: true, events);
        var factory = new FakeHotkeyServiceFactory(first, second);
        using var binding = new GlobalHotkeyBinding(factory);

        Assert.True(binding.TryApply(Parse("alt+space"), out _));
        Assert.True(binding.TryApply(Parse("alt+shift+space"), out _));

        Assert.True(events.IndexOf("second:start") < events.IndexOf("first:dispose"));
        Assert.True(first.Disposed);
        Assert.Equal(Parse("alt+shift+space"), binding.Gesture);
    }

    [Fact]
    public void TryApply_keeps_the_active_binding_when_the_replacement_cannot_register()
    {
        var events = new List<string>();
        var first = new FakeHotkeyService("first", startSucceeds: true, events);
        var rejected = new FakeHotkeyService("rejected", startSucceeds: false, events);
        var factory = new FakeHotkeyServiceFactory(first, rejected);
        using var binding = new GlobalHotkeyBinding(factory);
        Assert.True(binding.TryApply(Parse("alt+space"), out _));

        var applied = binding.TryApply(Parse("ctrl+alt+k"), out var error);

        Assert.False(applied);
        Assert.False(string.IsNullOrWhiteSpace(error));
        Assert.False(first.Disposed);
        Assert.True(rejected.Disposed);
        Assert.Equal(Parse("alt+space"), binding.Gesture);
    }

    private static GlobalHotkeyGesture Parse(string text)
    {
        Assert.True(GlobalHotkeyParser.TryParse(text, out var gesture, out var error), error);
        return gesture;
    }

    private sealed class FakeHotkeyServiceFactory(params FakeHotkeyService[] services) : IGlobalHotkeyServiceFactory
    {
        private readonly Queue<FakeHotkeyService> _services = new(services);

        public IGlobalHotkeyService Create(GlobalHotkeyGesture gesture) => _services.Dequeue();
    }

    private sealed class FakeHotkeyService(string name, bool startSucceeds, List<string> events) : IGlobalHotkeyService
    {
        public event EventHandler? HotkeyPressed;
        public bool Started { get; private set; }
        public bool Disposed { get; private set; }

        public bool Start()
        {
            Started = true;
            events.Add($"{name}:start");
            return startSucceeds;
        }

        private void RaiseHotkeyPressed() => HotkeyPressed?.Invoke(this, EventArgs.Empty);

        public void Stop() { }

        public void Dispose()
        {
            Disposed = true;
            events.Add($"{name}:dispose");
        }
    }
}
