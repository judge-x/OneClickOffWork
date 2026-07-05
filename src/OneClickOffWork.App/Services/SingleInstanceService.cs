using System.IO.Pipes;
using System.IO;
using System.Threading;

namespace OneClickOffWork.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly string _name;
    private Mutex? _mutex;
    private CancellationTokenSource? _cts;

    public event EventHandler? ShowRequested;

    public SingleInstanceService(string name) => _name = name;

    public bool TryAcquire()
    {
        _mutex = new Mutex(true, _name, out var created);
        if (!created) return false;
        _cts = new CancellationTokenSource();
        _ = ListenAsync(_cts.Token);
        return true;
    }

    public void NotifyExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _name, PipeDirection.Out);
            client.Connect(300);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine("show");
        }
        catch { }
    }

    private async Task ListenAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(_name, PipeDirection.In, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(token);
                using var reader = new StreamReader(server);
                var message = await reader.ReadLineAsync(token);
                if (message == "show")
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => ShowRequested?.Invoke(this, EventArgs.Empty));
                }
            }
            catch when (token.IsCancellationRequested) { }
            catch { await Task.Delay(500, token).ContinueWith(_ => { }); }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _mutex?.Dispose();
    }
}
