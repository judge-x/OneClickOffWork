using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using OneClickOffWork.ViewModels;

namespace OneClickOffWork;

public partial class MainWindow : Window
{
    private const int HOTKEY_ID = 9001;
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint VK_D = 0x44;
    private bool _allowExit;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Closing += MainWindow_Closing;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        source?.AddHook(WndProc);
        RegisterHotKey(source!.Handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT, VK_D);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID && DataContext is MainViewModel vm)
        {
            vm.ShowMainWindow();
            vm.OffWorkCommand.Execute(null);
            handled = true;
        }
        return IntPtr.Zero;
    }

    private async void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_allowExit) return;
        if (DataContext is not MainViewModel vm) return;
        if (App.Services.Settings.Current.MinimizeToTrayOnClose)
        {
            e.Cancel = true;
            Hide();
            if (!App.Services.Settings.Current.HasShownTrayCloseTip)
            {
                App.Services.Settings.Current.HasShownTrayCloseTip = true;
                await App.Services.Settings.SaveAsync();
                App.Services.Tray.ShowBalloon("一键下班", "软件将继续在后台运行，可在右下角托盘中找到。");
                App.Services.Notifications.Toast("软件已最小化到托盘");
            }
        }
        else
        {
            _allowExit = true;
            await vm.ExitAsync();
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (e.ClickCount == 2)
        {
            return;
        }
        try { DragMove(); } catch { }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    public void AllowExit() => _allowExit = true;

    protected override void OnClosed(EventArgs e)
    {
        var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        if (source is not null) UnregisterHotKey(source.Handle, HOTKEY_ID);
        base.OnClosed(e);
    }
}
