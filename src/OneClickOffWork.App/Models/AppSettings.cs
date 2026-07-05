namespace OneClickOffWork.Models;

public sealed class AppSettings
{
    public bool StartWithWindows { get; set; }
    public bool MinimizeToTrayOnClose { get; set; } = true;
    public bool AutoCountdownAfterClick { get; set; } = true;
    public int CountdownSeconds { get; set; } = 120;
    public string WeatherCity { get; set; } = "北京";
    public string Theme { get; set; } = "System";
    public bool ShowOnboarding { get; set; }
    public bool PlaySoundBeforePowerOff { get; set; } = true;
    public bool ShowNotificationBeforePowerOff { get; set; } = true;
    public bool HasShownTrayCloseTip { get; set; }
}
