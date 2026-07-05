using System.Windows;
using Microsoft.Win32;

namespace OneClickOffWork.Services;

public sealed class ThemeService
{
    public void Apply(string theme)
    {
        var dark = theme == "Dark" || (theme == "System" && IsSystemDark());
        var resources = System.Windows.Application.Current.Resources;
        resources["WindowBackgroundBrush"] = dark ? resources["DarkWindowBackgroundBrush"] : resources["LightWindowBackgroundBrush"];
        resources["CardBackgroundBrush"] = dark ? resources["DarkCardBackgroundBrush"] : resources["LightCardBackgroundBrush"];
        resources["TextBrush"] = dark ? resources["DarkTextBrush"] : resources["LightTextBrush"];
        resources["SubtleTextBrush"] = dark ? resources["DarkSubtleTextBrush"] : resources["LightSubtleTextBrush"];
        resources["BorderBrushSoft"] = dark ? resources["DarkBorderBrushSoft"] : resources["LightBorderBrushSoft"];
        resources["InputBackgroundBrush"] = dark ? resources["DarkInputBackgroundBrush"] : resources["LightInputBackgroundBrush"];
    }

    private static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return (int?)key?.GetValue("AppsUseLightTheme") == 0;
        }
        catch
        {
            return false;
        }
    }
}
