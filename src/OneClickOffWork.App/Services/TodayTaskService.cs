using OneClickOffWork.Models;

namespace OneClickOffWork.Services;

public sealed class TodayTaskService
{
    private readonly LogService _log;
    public List<TodayTaskItem> Items { get; set; } = new();

    public TodayTaskService(LogService log) => _log = log;

    public async Task LoadAsync()
    {
        Items = await JsonFileService.LoadOrCreateAsync(StoragePaths.TodayTasksFile, () => new List<TodayTaskItem>(), _log);
        NormalizeSort();
    }

    public async Task SaveAsync()
    {
        NormalizeSort();
        await JsonFileService.SaveAsync(StoragePaths.TodayTasksFile, Items);
        _log.Info("今日任务保存成功");
    }

    public List<TodayTaskItem> TodayItems()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Items
            .Where(x => x.WorkDate == today)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToList();
    }

    public async Task AddTodayTaskAsync(string title)
    {
        var cleaned = title.Trim();
        if (string.IsNullOrWhiteSpace(cleaned)) return;

        var todayCount = TodayItems().Count;
        Items.Add(new TodayTaskItem
        {
            Title = cleaned,
            WorkDate = DateOnly.FromDateTime(DateTime.Now),
            SortOrder = todayCount + 1
        });
        await SaveAsync();
    }

    public async Task DeleteAsync(TodayTaskItem item)
    {
        Items.RemoveAll(x => x.Id == item.Id);
        await SaveAsync();
    }

    private void NormalizeSort()
    {
        var grouped = Items
            .OrderBy(x => x.WorkDate)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .GroupBy(x => x.WorkDate);

        foreach (var group in grouped)
        {
            var index = 1;
            foreach (var item in group)
            {
                item.SortOrder = index++;
            }
        }
    }
}
