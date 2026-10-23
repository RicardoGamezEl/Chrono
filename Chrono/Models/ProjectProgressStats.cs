using System;

namespace Chrono.Models;

public class ProjectProgressStats
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectDescription { get; set; } = string.Empty;
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int NotStartedTasks { get; set; }
    public double ProgressRatio { get; set; } // 0.0 to 1.0 for ProgressBar
    public double ProgressPercentage { get; set; } // 0 to 100 for display
    public string FormattedProgress => $"{ProgressPercentage:F0}%";
    public TimeSpan TotalTime { get; set; }
    public string FormattedTotalTime => $"{(int)TotalTime.TotalHours:D2}:{TotalTime.Minutes:D2}:{TotalTime.Seconds:D2}";
    public string TasksSummary => $"{CompletedTasks}/{TotalTasks} tareas";
}
