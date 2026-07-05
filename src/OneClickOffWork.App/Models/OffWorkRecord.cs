namespace OneClickOffWork.Models;

public sealed class OffWorkRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime Time { get; set; } = DateTime.Now;
    public bool MonitorPowerCallSucceeded { get; set; }
    public string Source { get; set; } = "";
}
