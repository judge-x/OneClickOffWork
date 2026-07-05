using OneClickOffWork.Services;

namespace OneClickOffWork;

public sealed class AppServices
{
    public AppStateService State { get; } = new();
    public LogService Log { get; } = new();
    public SettingsService Settings { get; }
    public ReminderService Reminders { get; }
    public TodayTaskService TodayTasks { get; }
    public OffWorkRecordService OffWorkRecords { get; }
    public PowerService Power { get; } = new();
    public NotificationService Notifications { get; } = new();
    public WeatherService Weather { get; } = new();
    public StartupService Startup { get; } = new();
    public ThemeService Theme { get; } = new();
    public TrayService Tray { get; }

    public AppServices()
    {
        Settings = new SettingsService(Log);
        Reminders = new ReminderService(Log);
        TodayTasks = new TodayTaskService(Log);
        OffWorkRecords = new OffWorkRecordService(Log);
        Tray = new TrayService(Log, Notifications);
    }

    public async Task InitializeAsync()
    {
        await Log.InitializeAsync();
        await Settings.LoadAsync();
        await Reminders.LoadAsync();
        await TodayTasks.LoadAsync();
        await OffWorkRecords.LoadAsync();
    }
}
