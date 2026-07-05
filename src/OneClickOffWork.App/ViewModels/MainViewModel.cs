using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using OneClickOffWork.Commands;
using OneClickOffWork.Views;

namespace OneClickOffWork.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly AppServices _services;
    private readonly DispatcherTimer _calendarTimer;
    private object _currentView = null!;
    private string _calendarTitle = "";
    private string _toastText = "";
    private bool _isToastVisible;
    private bool _isFlowRunning;
    private DateOnly _calendarDate;

    public MainViewModel(AppServices services)
    {
        _services = services;
        VersionText = $"v{typeof(App).Assembly.GetName().Version?.ToString(3) ?? "1.0.0"}";
        _calendarDate = DateOnly.FromDateTime(DateTime.Now);
        _calendarTitle = DateTime.Now.ToString("yyyy年 M月");
        CalendarDays = BuildCalendarDays(DateTime.Now);
        _calendarTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(1)
        };
        _calendarTimer.Tick += (_, _) => RefreshComputed();
        _calendarTimer.Start();

        NavigateHomeCommand = new RelayCommand(NavigateHome);
        NavigateTodayTasksCommand = new RelayCommand(NavigateWorkAnalysis);
        NavigateRemindersCommand = new RelayCommand(NavigateReminders);
        NavigateSettingsCommand = new RelayCommand(NavigateSettings);
        NavigateOnboardingCommand = new RelayCommand(NavigateOnboarding);
        OffWorkCommand = new AsyncRelayCommand(StartOffWorkFlowAsync, () => !_isFlowRunning);
        AboutCommand = new RelayCommand(ShowAbout);

        services.Notifications.ToastRequested += async (_, message) => await ShowToastAsync(message);
        services.Tray.OpenRequested += (_, _) => ShowMainWindow();
        services.Tray.OffWorkRequested += (_, _) => OffWorkCommand.Execute(null);
        services.Tray.RemindersRequested += (_, _) => { ShowMainWindow(); NavigateReminders(); };
        services.Tray.SettingsRequested += (_, _) => { ShowMainWindow(); NavigateSettings(); };
        services.Tray.ExitRequested += async (_, _) => await ExitAsync();

        NavigateHome();
    }

    public ICommand NavigateHomeCommand { get; }
    public ICommand NavigateTodayTasksCommand { get; }
    public ICommand NavigateRemindersCommand { get; }
    public ICommand NavigateSettingsCommand { get; }
    public ICommand NavigateOnboardingCommand { get; }
    public ICommand OffWorkCommand { get; }
    public ICommand AboutCommand { get; }

    public string VersionText { get; }
    public string CalendarTitle
    {
        get => _calendarTitle;
        private set => SetProperty(ref _calendarTitle, value);
    }
    public ObservableCollection<CalendarDayViewModel> CalendarDays { get; }

    public object CurrentView
    {
        get => _currentView;
        private set
        {
            DisposeCurrentViewDataContext();
            SetProperty(ref _currentView, value);
        }
    }

    public string ToastText
    {
        get => _toastText;
        set => SetProperty(ref _toastText, value);
    }

    public bool IsToastVisible
    {
        get => _isToastVisible;
        set => SetProperty(ref _isToastVisible, value);
    }

    public void NavigateHome()
    {
        CurrentView = new HomePage { DataContext = new HomeViewModel(_services, this) };
    }

    public void NavigateWorkAnalysis()
    {
        CurrentView = new TodayTaskPage { DataContext = new WorkAnalysisViewModel(_services) };
    }

    public void NavigateReminders()
    {
        CurrentView = new ReminderManagePage { DataContext = new ReminderManageViewModel(_services, this) };
    }

    public void NavigateSettings()
    {
        CurrentView = new SettingsPage { DataContext = new SettingsViewModel(_services, this) };
    }

    public void NavigateOnboarding()
    {
        CurrentView = new OnboardingPage { DataContext = new OnboardingViewModel(_services, this) };
    }

    public async Task StartOffWorkFlowAsync()
    {
        if (_isFlowRunning) return;
        _isFlowRunning = true;
        _services.State.IsOffWorkFlowRunning = true;
        _services.State.Status = "下班确认进行中";
        _services.Log.Info("点击一键下班");
        _services.Notifications.Toast("下班流程开始");
        _services.Tray.ShowBalloon("一键下班", "下班确认流程已开始");

        try
        {
            var dialog = new ConfirmOffWorkDialog(
                _services.Reminders.EnabledItems(),
                _services.Settings.Current.CountdownSeconds,
                _services.Settings.Current.AutoCountdownAfterClick);

            var result = dialog.ShowDialog();
            if (result == true)
            {
                await ExecutePowerOffAsync(dialog.WasAutoConfirmed ? "自动倒计时执行息屏" : "用户确认下班");
            }
            else
            {
                _services.Log.Info("用户取消下班");
                _services.Notifications.Toast("已取消本次下班流程");
            }
        }
        finally
        {
            _isFlowRunning = false;
            _services.State.IsOffWorkFlowRunning = false;
            _services.State.Status = _services.State.LastOffWorkAt is null ? "正在运行 / 等待下班" : "已执行下班流程";
        }
    }

    public async Task ExecutePowerOffAsync(string logMessage)
    {
        try
        {
            _services.Log.Info(logMessage);
            if (_services.Settings.Current.PlaySoundBeforePowerOff) _services.Notifications.Beep();
            if (_services.Settings.Current.ShowNotificationBeforePowerOff)
            {
                _services.Notifications.Toast("即将息屏，不会关机、注销或睡眠");
                _services.Tray.ShowBalloon("一键下班", "即将关闭显示器");
            }

            await Task.Delay(350);
            var ok = await _services.Power.TurnOffMonitorAsync(GetMainWindowHandle());
            var offWorkAt = DateTime.Now;
            _services.State.LastOffWorkAt = offWorkAt;
            await _services.OffWorkRecords.AddAsync(offWorkAt, ok, logMessage);
            _services.Log.Info("息屏 API 调用结果", ok ? "Success" : "Failed");
            _services.Notifications.Toast(ok ? "已执行息屏" : "息屏 API 调用失败");
        }
        catch (Exception ex)
        {
            _services.Log.Error("执行息屏流程失败", ex);
            _services.Notifications.Toast("执行息屏失败，已记录日志");
        }
    }

    private static IntPtr GetMainWindowHandle()
    {
        if (System.Windows.Application.Current.MainWindow is not { } window)
        {
            return IntPtr.Zero;
        }

        var helper = new WindowInteropHelper(window);
        return helper.Handle != IntPtr.Zero ? helper.Handle : helper.EnsureHandle();
    }

    public void ShowMainWindow()
    {
        if (System.Windows.Application.Current.MainWindow is not { } window) return;
        window.Show();
        window.WindowState = System.Windows.WindowState.Normal;
        window.Activate();
    }

    public async Task ExitAsync()
    {
        _services.Log.Info("用户退出软件");
        _services.Tray.Dispose();
        await Task.Delay(80);
        if (System.Windows.Application.Current.MainWindow is MainWindow window) window.AllowExit();
        System.Windows.Application.Current.Shutdown();
    }

    public async Task ShowToastAsync(string message)
    {
        ToastText = message;
        IsToastVisible = true;
        await Task.Delay(2600);
        IsToastVisible = false;
    }

    public void RefreshComputed()
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        if (today == _calendarDate && CalendarTitle == now.ToString("yyyy年 M月"))
        {
            return;
        }

        _calendarDate = today;
        CalendarTitle = now.ToString("yyyy年 M月");
        CalendarDays.Clear();
        foreach (var day in BuildCalendarDays(now))
        {
            CalendarDays.Add(day);
        }
    }

    private void ShowAbout()
    {
        System.Windows.MessageBox.Show(
            $"一键下班 {VersionText}\n\n一个轻量级本地 Windows 工具。",
            "关于一键下班",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private void DisposeCurrentViewDataContext()
    {
        if (_currentView is System.Windows.Controls.UserControl { DataContext: IDisposable disposable })
        {
            disposable.Dispose();
        }
    }

    private static ObservableCollection<CalendarDayViewModel> BuildCalendarDays(DateTime now)
    {
        var first = new DateTime(now.Year, now.Month, 1);
        var leading = ((int)first.DayOfWeek + 6) % 7;
        var days = DateTime.DaysInMonth(now.Year, now.Month);
        var result = new ObservableCollection<CalendarDayViewModel>();

        for (var i = 0; i < leading; i++)
        {
            result.Add(new CalendarDayViewModel());
        }

        for (var day = 1; day <= days; day++)
        {
            result.Add(new CalendarDayViewModel
            {
                Text = day.ToString(),
                IsToday = day == now.Day
            });
        }

        return result;
    }
}
