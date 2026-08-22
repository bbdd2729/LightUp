using LightUpUI.Services;

namespace LightUpTest.Startup;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task Secondary_instance_signals_the_primary_instance()
    {
        var applicationId = $"LightUpTest.{Guid.NewGuid():N}";
        using var primary = new SingleInstanceCoordinator(applicationId);
        using var secondary = new SingleInstanceCoordinator(applicationId);
        var activation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        primary.ActivationRequested += (_, _) => activation.TrySetResult();
        primary.StartListening();

        var delivered = secondary.TrySignalPrimary(TimeSpan.FromSeconds(2));
        await activation.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);

        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);
        Assert.True(delivered);
    }

    [Fact]
    public void Primary_instance_does_not_signal_itself()
    {
        using var instance = new SingleInstanceCoordinator($"LightUpTest.{Guid.NewGuid():N}");

        Assert.True(instance.IsPrimaryInstance);
        Assert.False(instance.TrySignalPrimary(TimeSpan.FromMilliseconds(10)));
    }
}
