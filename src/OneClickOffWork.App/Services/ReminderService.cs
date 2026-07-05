using OneClickOffWork.Models;

namespace OneClickOffWork.Services;

public sealed class ReminderService
{
    private readonly LogService _log;
    public List<ReminderItem> Items { get; set; } = new();

    public ReminderService(LogService log) => _log = log;

    public async Task LoadAsync()
    {
        Items = await JsonFileService.LoadOrCreateAsync(StoragePaths.RemindersFile, DefaultReminders, _log);
        NormalizeSort();
    }

    public async Task SaveAsync()
    {
        NormalizeSort();
        await JsonFileService.SaveAsync(StoragePaths.RemindersFile, Items);
        _log.Info("注意事项保存成功");
    }

    public List<ReminderItem> EnabledItems()
        => Items.Where(x => x.IsEnabled).OrderBy(x => x.SortOrder).ToList();

    public async Task RestoreDefaultsAsync()
    {
        Items = DefaultReminders();
        await SaveAsync();
    }

    public async Task ExportAsync(string path) => await JsonFileService.SaveAsync(path, Items);

    public async Task ImportAsync(string path)
    {
        Items = await JsonFileService.LoadOrCreateAsync(path, DefaultReminders, _log);
        NormalizeSort();
        await SaveAsync();
    }

    private void NormalizeSort()
    {
        Items = Items.OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt).ToList();
        for (var i = 0; i < Items.Count; i++) Items[i].SortOrder = i + 1;
    }

    private static List<ReminderItem> DefaultReminders()
    {
        var titles = new[]
        {
            ("是否关闭公司内部系统？", "确认已退出不再使用的内部系统，避免敏感页面长时间保持登录。"),
            ("是否保存所有文档？", "检查办公文档、表格、设计稿和临时文件是否已经保存。"),
            ("是否提交日报？", "确认日报、周报或团队同步内容已经提交。"),
            ("是否关闭空调、灯光、门窗？", "离开工位前检查办公室环境与安全事项。"),
            ("是否检查明天会议安排？", "查看日历和待办，确认明天的重要事项。"),
            ("是否同步代码或提交 Git？", "推送必要代码，避免本地修改只留在电脑上。"),
            ("是否退出敏感账号？", "退出财务、管理后台、生产环境等敏感账号。")
        };

        return titles.Select((x, i) => new ReminderItem
        {
            Title = x.Item1,
            Content = x.Item2,
            SortOrder = i + 1
        }).ToList();
    }
}
