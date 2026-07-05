using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using OneClickOffWork.Commands;
using OneClickOffWork.Models;

namespace OneClickOffWork.ViewModels;

public sealed class HomeViewModel : ViewModelBase, IDisposable
{
    private static readonly TimeSpan WeatherRefreshInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan WeatherRetryInterval = TimeSpan.FromMinutes(5);
    private static readonly string[] Quotes =
    {
        "清晰的任务，是一整天最好的开场。",
        "少一点切换，多一点完成。",
        "把计划写下来，脑子就能留给真正重要的事。",
        "先处理关键任务，剩下的时间会更从容。",
        "今天完成一点点，也是在给未来减压。"
    };

    private readonly AppServices _services;
    private readonly MainViewModel _main;
    private readonly DispatcherTimer _refreshTimer;
    private string _newTaskTitle = "";
    private string _weatherCity = "本地天气";
    private string _weatherDescription = "正在获取";
    private string _weatherTemperature = "--";
    private string _weatherIcon = "◌";
    private DateOnly _currentDate;
    private DateTime? _lastWeatherAttemptAt;
    private bool _lastWeatherSucceeded;
    private bool _isWeatherLoading;

    public HomeViewModel(AppServices services, MainViewModel main)
    {
        _services = services;
        _main = main;
        _currentDate = DateOnly.FromDateTime(DateTime.Now);
        WorkQuote = Quotes[Random.Shared.Next(Quotes.Length)];
        TodayTasks = new ObservableCollection<TodayTaskItem>(_services.TodayTasks.TodayItems());
        ReminderPreviewItems = new ObservableCollection<ReminderItem>(
            _services.Reminders.EnabledItems().Take(6));
        AddTaskCommand = new AsyncRelayCommand(AddTaskAsync, () => !string.IsNullOrWhiteSpace(NewTaskTitle));
        SaveTaskCommand = new AsyncRelayCommand(SaveTasksAsync);
        DeleteTaskCommand = new AsyncRelayCommand<TodayTaskItem>(DeleteTaskAsync);
        RefreshWeatherCommand = new AsyncRelayCommand(() => LoadWeatherAsync(showLoading: true, force: true));
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _refreshTimer.Tick += async (_, _) => await RefreshForClockAsync();
        _refreshTimer.Start();
        _ = LoadWeatherAsync(showLoading: true, force: true);
    }

    public ICommand OffWorkCommand => _main.OffWorkCommand;
    public ICommand NavigateRemindersCommand => _main.NavigateRemindersCommand;
    public ICommand AddTaskCommand { get; }
    public ICommand SaveTaskCommand { get; }
    public ICommand DeleteTaskCommand { get; }
    public ICommand RefreshWeatherCommand { get; }

    public ObservableCollection<TodayTaskItem> TodayTasks { get; }
    public ObservableCollection<ReminderItem> ReminderPreviewItems { get; }
    public string WorkQuote { get; }
    public string TodayCompletionText => $"{TodayTasks.Count(x => x.IsCompleted)} / {TodayTasks.Count}";
    public string TodayDateText => _currentDate.ToDateTime(TimeOnly.MinValue).ToString("yyyy年 M月 d日");
    public string TodayWeekText => $"星期{new[] { "日", "一", "二", "三", "四", "五", "六" }[(int)_currentDate.DayOfWeek]}";
    public string TodayProgressText => $"已完成 {TodayTasks.Count(x => x.IsCompleted)} 项";

    public string WeatherCity
    {
        get => _weatherCity;
        private set => SetProperty(ref _weatherCity, value);
    }

    public string WeatherDescription
    {
        get => _weatherDescription;
        private set => SetProperty(ref _weatherDescription, value);
    }

    public string WeatherTemperature
    {
        get => _weatherTemperature;
        private set => SetProperty(ref _weatherTemperature, value);
    }

    public string WeatherIcon
    {
        get => _weatherIcon;
        private set => SetProperty(ref _weatherIcon, value);
    }

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set
        {
            if (SetProperty(ref _newTaskTitle, value) && AddTaskCommand is AsyncRelayCommand command)
            {
                command.RaiseCanExecuteChanged();
            }
        }
    }

    private async Task RefreshForClockAsync()
    {
        RefreshTodayIfNeeded();

        if (ShouldRefreshWeather())
        {
            await LoadWeatherAsync(showLoading: false, force: false);
        }
    }

    private bool ShouldRefreshWeather()
    {
        if (_isWeatherLoading)
        {
            return false;
        }

        if (_lastWeatherAttemptAt is null)
        {
            return true;
        }

        var elapsed = DateTime.Now - _lastWeatherAttemptAt.Value;
        return _lastWeatherSucceeded
            ? elapsed >= WeatherRefreshInterval
            : elapsed >= WeatherRetryInterval;
    }

    private void RefreshTodayIfNeeded()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (today == _currentDate)
        {
            return;
        }

        _currentDate = today;
        ReloadTasks();
        OnPropertyChanged(nameof(TodayDateText));
        OnPropertyChanged(nameof(TodayWeekText));
        _main.RefreshComputed();
    }

    private async Task LoadWeatherAsync(bool showLoading, bool force)
    {
        if (_isWeatherLoading || (!force && !ShouldRefreshWeather()))
        {
            return;
        }

        _isWeatherLoading = true;
        _lastWeatherAttemptAt = DateTime.Now;

        if (showLoading)
        {
            WeatherCity = "刷新中";
            WeatherDescription = "正在获取";
            WeatherTemperature = "--";
            WeatherIcon = "◌";
        }

        var weather = await _services.Weather.GetCurrentWeatherAsync(_services.Settings.Current.WeatherCity);
        _lastWeatherSucceeded = weather.IsAvailable;
        WeatherCity = string.IsNullOrWhiteSpace(weather.City) ? "本地天气" : weather.City;
        WeatherDescription = weather.Description;
        WeatherTemperature = weather.Temperature;
        WeatherIcon = weather.Icon;
        _isWeatherLoading = false;
    }

    private async Task AddTaskAsync()
    {
        RefreshTodayIfNeeded();
        await _services.TodayTasks.AddTodayTaskAsync(NewTaskTitle);
        NewTaskTitle = "";
        ReloadTasks();
        _services.Notifications.Toast("今日任务已添加");
    }

    private async Task SaveTasksAsync()
    {
        RefreshTodayIfNeeded();
        _services.TodayTasks.Items.RemoveAll(x => x.WorkDate == _currentDate);
        _services.TodayTasks.Items.AddRange(TodayTasks);
        await _services.TodayTasks.SaveAsync();
        OnPropertyChanged(nameof(TodayCompletionText));
        OnPropertyChanged(nameof(TodayProgressText));
    }

    private async Task DeleteTaskAsync(TodayTaskItem? item)
    {
        RefreshTodayIfNeeded();
        if (item is null) return;
        await _services.TodayTasks.DeleteAsync(item);
        ReloadTasks();
        _services.Notifications.Toast("今日任务已删除");
    }

    private void ReloadTasks()
    {
        TodayTasks.Clear();
        foreach (var item in _services.TodayTasks.TodayItems()) TodayTasks.Add(item);
        OnPropertyChanged(nameof(TodayCompletionText));
        OnPropertyChanged(nameof(TodayProgressText));
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
    }
}
