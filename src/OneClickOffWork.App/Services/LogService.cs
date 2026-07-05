using OneClickOffWork.Models;

namespace OneClickOffWork.Services;

public sealed class LogService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private List<LogEntry> _entries = new();

    public async Task InitializeAsync()
    {
        _entries = await JsonFileService.LoadOrCreateAsync(StoragePaths.LogsFile, () => new List<LogEntry>());
    }

    public void Info(string message, string? detail = null) => _ = WriteAsync("Info", message, detail);
    public void Warn(string message, string? detail = null) => _ = WriteAsync("Warn", message, detail);
    public void Error(string message, Exception ex) => _ = WriteAsync("Error", message, ex.ToString());

    private async Task WriteAsync(string level, string message, string? detail)
    {
        await _gate.WaitAsync();
        try
        {
            _entries.Add(new LogEntry { Level = level, Message = message, Detail = detail });
            if (_entries.Count > 1000) _entries = _entries.TakeLast(1000).ToList();
            await JsonFileService.SaveAsync(StoragePaths.LogsFile, _entries);
        }
        catch
        {
            // Logging must never crash the app.
        }
        finally
        {
            _gate.Release();
        }
    }
}
