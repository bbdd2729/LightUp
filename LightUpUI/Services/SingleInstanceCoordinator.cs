using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace LightUpUI.Services;

public sealed class SingleInstanceCoordinator : IDisposable
{
    private readonly Mutex _mutex;
    private readonly string _pipeName;
    private readonly CancellationTokenSource _listenerCancellation = new();
    private Task? _listenerTask;
    private bool _disposed;

    public SingleInstanceCoordinator(string applicationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        var identifier = applicationId.Replace(' ', '.');
        _pipeName = $"{identifier}.activation";
        _mutex = new Mutex(true, $"Local\\{identifier}.single-instance", out var createdNew);
        IsPrimaryInstance = createdNew;
    }

    public bool IsPrimaryInstance { get; }

    public event EventHandler? ActivationRequested;

    public void StartListening()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!IsPrimaryInstance || _listenerTask is not null)
            return;

        _listenerTask = Task.Run(ListenAsync);
    }

    public bool TrySignalPrimary(TimeSpan timeout)
    {
        if (IsPrimaryInstance || _disposed)
            return false;

        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out);
            client.Connect(Math.Max(1, (int)timeout.TotalMilliseconds));
            client.WriteByte(1);
            client.Flush();
            return true;
        }
        catch (TimeoutException) { return false; }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    private async Task ListenAsync()
    {
        while (!_listenerCancellation.IsCancellationRequested)
        {
            using var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            try
            {
                await server.WaitForConnectionAsync(_listenerCancellation.Token).ConfigureAwait(false);
                if (server.ReadByte() >= 0)
                    ActivationRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException) when (_listenerCancellation.IsCancellationRequested)
            {
                return;
            }
            catch (IOException)
            {
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _listenerCancellation.Cancel();
        if (_listenerTask?.Wait(TimeSpan.FromMilliseconds(100)) != false)
            _listenerCancellation.Dispose();
        _mutex.Dispose();
    }
}
