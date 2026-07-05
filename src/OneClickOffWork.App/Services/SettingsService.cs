using OneClickOffWork.Models;

namespace OneClickOffWork.Services;

public sealed class SettingsService
{
    private readonly LogService _log;
    public AppSettings Current { get; set; } = new();

    public SettingsService(LogService log) => _log = log;

    public async Task LoadAsync()
    {
        Current = await JsonFileService.LoadOrCreateAsync(StoragePaths.SettingsFile, () => new AppSettings(), _log);
    }

    public async Task SaveAsync()
    {
        Current.CountdownSeconds = Math.Clamp(Current.CountdownSeconds, 10, 3600);
        await JsonFileService.SaveAsync(StoragePaths.SettingsFile, Current);
        _log.Info("设置保存成功");
    }

    public async Task RestoreDefaultsAsync()
    {
        Current = new AppSettings { ShowOnboarding = false };
        await SaveAsync();
    }
}
