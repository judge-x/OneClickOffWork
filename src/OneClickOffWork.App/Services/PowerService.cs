using System.Runtime.InteropServices;

namespace OneClickOffWork.Services;

public sealed class PowerService
{
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;
    private const int MONITOR_OFF = 2;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    public async Task<bool> TurnOffMonitorAsync(IntPtr ownerWindowHandle)
    {
        if (ownerWindowHandle == IntPtr.Zero)
        {
            return false;
        }

        return await Task.Run(() => TurnOffMonitorCore(ownerWindowHandle));
    }

    private static bool TurnOffMonitorCore(IntPtr ownerWindowHandle)
    {
        var result = SendMessageTimeout(
            ownerWindowHandle,
            WM_SYSCOMMAND,
            (IntPtr)SC_MONITORPOWER,
            (IntPtr)MONITOR_OFF,
            SMTO_ABORTIFHUNG,
            500,
            out _);

        return result != IntPtr.Zero || Marshal.GetLastWin32Error() == 0;
    }
}
