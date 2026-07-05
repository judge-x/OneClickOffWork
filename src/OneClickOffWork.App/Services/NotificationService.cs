using System.Media;
using System.Windows;

namespace OneClickOffWork.Services;

public sealed class NotificationService
{
    public event EventHandler<string>? ToastRequested;

    public void Toast(string message)
    {
        ToastRequested?.Invoke(this, message);
    }

    public void Beep()
    {
        try { SystemSounds.Asterisk.Play(); } catch { }
    }

    public void Message(string title, string message)
    {
        System.Windows.MessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
