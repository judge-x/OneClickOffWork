using System.Windows.Input;
using Microsoft.Win32;
using OneClickOffWork.Commands;

namespace OneClickOffWork.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly MainViewModel _main;

    public SettingsViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        BackupCommand = new AsyncRelayCommand(BackupAsync);
        RestoreCommand = new AsyncRelayCommand(RestoreAsync);
        RestoreDefaultsCommand = new AsyncRelayCommand(RestoreDefaultsAsync);
    }

    public ICommand SaveCommand { get; }
    public ICommand BackupCommand { get; }
    public ICommand RestoreCommand { get; }
    public ICommand RestoreDefaultsCommand { get; }

    public bool StartWithWindows { get => _services.Settings.Current.StartWithWindows; set { _services.Settings.Current.StartWithWindows = value; OnPropertyChanged(); } }
    public bool MinimizeToTrayOnClose { get => _services.Settings.Current.MinimizeToTrayOnClose; set { _services.Settings.Current.MinimizeToTrayOnClose = value; OnPropertyChanged(); } }
    public bool AutoCountdownAfterClick { get => _services.Settings.Current.AutoCountdownAfterClick; set { _services.Settings.Current.AutoCountdownAfterClick = value; OnPropertyChanged(); } }
    public int CountdownSeconds { get => _services.Settings.Current.CountdownSeconds; set { _services.Settings.Current.CountdownSeconds = value; OnPropertyChanged(); } }
    public string WeatherCity { get => _services.Settings.Current.WeatherCity; set { _services.Settings.Current.WeatherCity = value; OnPropertyChanged(); } }
    public string Theme { get => _services.Settings.Current.Theme; set { _services.Settings.Current.Theme = value; OnPropertyChanged(); } }
    public bool ShowOnboarding { get => _services.Settings.Current.ShowOnboarding; set { _services.Settings.Current.ShowOnboarding = value; OnPropertyChanged(); } }
    public bool PlaySoundBeforePowerOff { get => _services.Settings.Current.PlaySoundBeforePowerOff; set { _services.Settings.Current.PlaySoundBeforePowerOff = value; OnPropertyChanged(); } }
    public bool ShowNotificationBeforePowerOff { get => _services.Settings.Current.ShowNotificationBeforePowerOff; set { _services.Settings.Current.ShowNotificationBeforePowerOff = value; OnPropertyChanged(); } }

    private async Task SaveAsync()
    {
        try
        {
            _services.Startup.SetEnabled(StartWithWindows);
            await _services.Settings.SaveAsync();
            _services.Theme.Apply(Theme);
            _services.Notifications.Toast("设置保存成功");
            _main.RefreshComputed();
        }
        catch (Exception ex)
        {
            _services.Log.Error("设置保存失败", ex);
            _services.Notifications.Toast("设置保存失败");
        }
    }

    private async Task BackupAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "JSON 文件|*.json", FileName = "settings.json" };
        if (dialog.ShowDialog() != true) return;
        await OneClickOffWork.Services.JsonFileService.SaveAsync(dialog.FileName, _services.Settings.Current);
        _services.Notifications.Toast("数据备份成功");
    }

    private async Task RestoreAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "JSON 文件|*.json" };
        if (dialog.ShowDialog() != true) return;
        _services.Settings.Current = await OneClickOffWork.Services.JsonFileService.LoadOrCreateAsync(dialog.FileName, () => _services.Settings.Current, _services.Log);
        await SaveAsync();
        NotifyAll();
        _services.Notifications.Toast("数据恢复成功");
    }

    private async Task RestoreDefaultsAsync()
    {
        await _services.Settings.RestoreDefaultsAsync();
        _services.Theme.Apply(Theme);
        NotifyAll();
        _services.Notifications.Toast("已恢复默认设置");
    }

    private void NotifyAll()
    {
        OnPropertyChanged(nameof(StartWithWindows));
        OnPropertyChanged(nameof(MinimizeToTrayOnClose));
        OnPropertyChanged(nameof(AutoCountdownAfterClick));
        OnPropertyChanged(nameof(CountdownSeconds));
        OnPropertyChanged(nameof(WeatherCity));
        OnPropertyChanged(nameof(Theme));
        OnPropertyChanged(nameof(ShowOnboarding));
        OnPropertyChanged(nameof(PlaySoundBeforePowerOff));
        OnPropertyChanged(nameof(ShowNotificationBeforePowerOff));
    }
}
