using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using OneClickOffWork.Models;

namespace OneClickOffWork.Views;

public partial class ConfirmOffWorkDialog : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private int _remainingSeconds;

    public ConfirmOffWorkDialog(IReadOnlyList<ReminderItem> reminders, int countdownSeconds, bool autoCountdown)
    {
        InitializeComponent();
        ReminderList.ItemsSource = reminders.Count == 0
            ? new[] { new ReminderItem { Title = "暂无启用注意事项", Content = "你仍然可以确认下班并执行息屏。", SortOrder = 1 } }
            : reminders;

        _remainingSeconds = Math.Clamp(countdownSeconds, 10, 3600);
        UpdateCountdown();
        if (autoCountdown)
        {
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }
        else
        {
            CountdownText.Text = "手动确认";
        }
    }

    public bool WasAutoConfirmed { get; private set; }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _remainingSeconds--;
        UpdateCountdown();
        if (_remainingSeconds <= 0)
        {
            WasAutoConfirmed = true;
            _timer.Stop();
            DialogResult = true;
            Close();
        }
    }

    private void UpdateCountdown()
    {
        CountdownText.Text = $"{_remainingSeconds / 60:00}:{_remainingSeconds % 60:00}";
        CountdownText.Foreground = _remainingSeconds <= 10
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 75, 95))
            : (System.Windows.Media.Brush)FindResource("TextBrush");
        CountdownText.Opacity = _remainingSeconds <= 10 && _remainingSeconds % 2 == 0 ? 0.65 : 1;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        DialogResult = false;
        Close();
    }
}
