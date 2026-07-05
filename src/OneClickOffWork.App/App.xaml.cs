using System.Windows;
using Microsoft.Win32;
using OneClickOffWork.Services;
using OneClickOffWork.ViewModels;

namespace OneClickOffWork;

public partial class App : System.Windows.Application
{
    public static AppServices Services { get; private set; } = null!;
    private SingleInstanceService? _singleInstance;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            Services?.Log.Error("UI 未处理异常", args.Exception);
            Services?.Notifications.Toast("应用遇到异常，已记录日志");
            if (MainWindow is null)
            {
                args.Handled = true;
                Shutdown(-1);
                return;
            }
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex) Services?.Log.Error("全局未处理异常", ex);
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Services?.Log.Error("后台任务异常", args.Exception);
            args.SetObserved();
        };

        _singleInstance = new SingleInstanceService("OneClickOffWork.SingleInstance");
        if (!_singleInstance.TryAcquire())
        {
            _singleInstance.NotifyExistingInstance();
            Shutdown();
            return;
        }

        Services = new AppServices();
        await Services.InitializeAsync();
        Services.Log.Info("软件启动");
        SystemEvents.PowerModeChanged += OnPowerModeChanged;

        var mainWindow = new MainWindow
        {
            DataContext = new MainViewModel(Services)
        };
        MainWindow = mainWindow;
        _singleInstance.ShowRequested += (_, _) =>
        {
            mainWindow.Show();
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        };
        Services.Tray.Initialize(mainWindow);
        Services.Theme.Apply(Services.Settings.Current.Theme);
        mainWindow.Show();

        Services.Settings.Current.ShowOnboarding = false;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        Services?.Log.Info("软件退出");
        Services?.Tray.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        Services?.Log.Info($"电源状态变化：{e.Mode}");
        if (e.Mode == PowerModes.Resume)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Services?.Notifications.Toast("已恢复显示，欢迎开始上班");
            });
        }
    }
}
