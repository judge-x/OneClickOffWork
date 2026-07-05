using OneClickOffWork.Models;

namespace OneClickOffWork.Services;

public sealed class OffWorkRecordService
{
    private readonly LogService _log;

    public OffWorkRecordService(LogService log) => _log = log;

    public List<OffWorkRecord> Items { get; set; } = new();

    public async Task LoadAsync()
    {
        Items = await JsonFileService.LoadOrCreateAsync(StoragePaths.OffWorkRecordsFile, () => new List<OffWorkRecord>(), _log);
        TrimOldRecords();
    }

    public async Task AddAsync(DateTime time, bool monitorPowerCallSucceeded, string source)
    {
        Items.Add(new OffWorkRecord
        {
            Time = time,
            MonitorPowerCallSucceeded = monitorPowerCallSucceeded,
            Source = source
        });

        TrimOldRecords();
        await JsonFileService.SaveAsync(StoragePaths.OffWorkRecordsFile, Items);
    }

    private void TrimOldRecords()
    {
        Items = Items
            .OrderBy(x => x.Time)
            .TakeLast(1000)
            .ToList();
    }
}
