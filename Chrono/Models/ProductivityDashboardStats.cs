using System;
using System.Collections.Generic;

namespace Chrono.Models;

public class ProductivityDashboardStats
{
    public TimeSpan TotalLoggedTime { get; set; } = TimeSpan.Zero;
    public string FormattedTotalTime => $"{(int)TotalLoggedTime.TotalHours:D2}:{TotalLoggedTime.Minutes:D2}:{TotalLoggedTime.Seconds:D2}";

    public TimeSpan TodayLoggedTime { get; set; } = TimeSpan.Zero;
    public string FormattedTodayTime => $"{(int)TodayLoggedTime.TotalHours:D2}:{TodayLoggedTime.Minutes:D2}:{TodayLoggedTime.Seconds:D2}";

    public TimeSpan WeekLoggedTime { get; set; } = TimeSpan.Zero;
    public string FormattedWeekTime => $"{(int)WeekLoggedTime.TotalHours:D2}:{WeekLoggedTime.Minutes:D2}:{WeekLoggedTime.Seconds:D2}";

    public int TotalProjects { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int PendingTasks { get; set; }

    public double GlobalProgressRatio { get; set; } // 0.0 to 1.0
    public double GlobalProgressPercentage { get; set; } // 0 to 100
    public string FormattedGlobalProgress => $"{GlobalProgressPercentage:F0}%";

    public List<ProjectProgressStats> Projects { get; set; } = new();
    public List<RecentActivityItem> RecentActivities { get; set; } = new();

    public bool HasProjects => TotalProjects > 0;
    public bool HasActivities => RecentActivities.Count > 0;
}
