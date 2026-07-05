namespace OneClickOffWork.Models;

public sealed class LogEntry
{
    public DateTime Time { get; set; } = DateTime.Now;
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = "";
    public string? Detail { get; set; }
}
