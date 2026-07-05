namespace OneClickOffWork.Services;

public sealed class AppStateService
{
    public string Status { get; set; } = "正在运行 / 等待下班";
    public bool IsOffWorkFlowRunning { get; set; }
    public DateTime? LastOffWorkAt { get; set; }
}
