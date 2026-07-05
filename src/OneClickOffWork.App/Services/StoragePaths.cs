namespace OneClickOffWork.Services;

public static class StoragePaths
{
    public static string AppDataDirectory { get; } =
        System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneClickOffWork");

    public static string SettingsFile => System.IO.Path.Combine(AppDataDirectory, "settings.json");
    public static string RemindersFile => System.IO.Path.Combine(AppDataDirectory, "reminders.json");
    public static string TodayTasksFile => System.IO.Path.Combine(AppDataDirectory, "today_tasks.json");
    public static string OffWorkRecordsFile => System.IO.Path.Combine(AppDataDirectory, "offwork_records.json");
    public static string LogsFile => System.IO.Path.Combine(AppDataDirectory, "logs.json");
}
