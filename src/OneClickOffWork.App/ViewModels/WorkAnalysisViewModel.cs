using OneClickOffWork.Models;

namespace OneClickOffWork.ViewModels;

public sealed class WorkAnalysisViewModel
{
    public WorkAnalysisViewModel(AppServices services)
    {
        var items = services.TodayTasks.Items;
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var monthItems = items.Where(x => x.WorkDate.Year == now.Year && x.WorkDate.Month == now.Month).ToList();
        var yearItems = items.Where(x => x.WorkDate.Year == now.Year).ToList();
        var offWorkRecords = services.OffWorkRecords.Items;
        var todayOffWork = offWorkRecords
            .Where(x => DateOnly.FromDateTime(x.Time) == today)
            .OrderByDescending(x => x.Time)
            .FirstOrDefault();
        var monthOffWorkRecords = offWorkRecords
            .Where(x => x.Time.Year == now.Year && x.Time.Month == now.Month)
            .ToList();

        TodayText = BuildRatio(items.Where(x => x.WorkDate == today));
        TodayOffWorkText = todayOffWork is null ? "未记录" : todayOffWork.Time.ToString("HH:mm");
        MonthAverageOffWorkText = BuildAverageOffWorkText(monthOffWorkRecords);
        MonthText = BuildRatio(monthItems);
        YearText = BuildRatio(yearItems);
        MonthPercent = Percent(monthItems);
        YearPercent = Percent(yearItems);
        MonthBars = BuildWeekBars(monthItems, now.Year, now.Month);
        YearBars = BuildMonthBars(yearItems);
    }

    public string TodayText { get; }
    public string TodayOffWorkText { get; }
    public string MonthAverageOffWorkText { get; }
    public string MonthText { get; }
    public string YearText { get; }
    public double MonthPercent { get; }
    public double YearPercent { get; }
    public IReadOnlyList<AnalysisBarViewModel> MonthBars { get; }
    public IReadOnlyList<AnalysisBarViewModel> YearBars { get; }

    private static string BuildRatio(IEnumerable<TodayTaskItem> source)
    {
        var list = source.ToList();
        return $"{list.Count(x => x.IsCompleted)} / {list.Count}";
    }

    private static double Percent(IReadOnlyCollection<TodayTaskItem> items)
    {
        if (items.Count == 0) return 0;
        return Math.Round(items.Count(x => x.IsCompleted) * 100.0 / items.Count, 1);
    }

    private static string BuildAverageOffWorkText(IReadOnlyCollection<OffWorkRecord> records)
    {
        if (records.Count == 0)
        {
            return "未记录";
        }

        var averageSeconds = records.Average(x => x.Time.TimeOfDay.TotalSeconds);
        return TimeSpan.FromSeconds(averageSeconds).ToString(@"hh\:mm");
    }

    private static IReadOnlyList<AnalysisBarViewModel> BuildWeekBars(IReadOnlyList<TodayTaskItem> monthItems, int year, int month)
    {
        var days = DateTime.DaysInMonth(year, month);
        var result = new List<AnalysisBarViewModel>();
        for (var week = 0; week < 5; week++)
        {
            var start = week * 7 + 1;
            var end = Math.Min(start + 6, days);
            if (start > days) break;
            var weekItems = monthItems.Where(x => x.WorkDate.Day >= start && x.WorkDate.Day <= end).ToList();
            result.Add(AnalysisBarViewModel.Create($"第 {week + 1} 周", weekItems));
        }
        return result;
    }

    private static IReadOnlyList<AnalysisBarViewModel> BuildMonthBars(IReadOnlyList<TodayTaskItem> yearItems)
    {
        return Enumerable.Range(1, 12)
            .Select(month =>
            {
                var monthItems = yearItems.Where(x => x.WorkDate.Month == month).ToList();
                return AnalysisBarViewModel.Create($"{month}月", monthItems);
            })
            .ToList();
    }
}

public sealed class AnalysisBarViewModel
{
    public string Label { get; private init; } = "";
    public string Text { get; private init; } = "0 / 0";
    public double Width { get; private init; }

    public static AnalysisBarViewModel Create(string label, IReadOnlyCollection<TodayTaskItem> items)
    {
        var completed = items.Count(x => x.IsCompleted);
        var total = items.Count;
        var width = total == 0 ? 6 : Math.Max(8, completed * 220.0 / total);
        return new AnalysisBarViewModel
        {
            Label = label,
            Text = $"{completed} / {total}",
            Width = width
        };
    }
}
