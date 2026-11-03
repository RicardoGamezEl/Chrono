using System;

namespace Chrono.Models;

public class ActiveSessionState
{
    public TaskItem Task { get; set; } = null!;
    public Project? Project { get; set; }
    public TimeEntry Entry { get; set; } = null!;
    public TimeSpan ElapsedCurrentSession { get; set; } = TimeSpan.Zero;
    public TimeSpan PriorCompletedDuration { get; set; } = TimeSpan.Zero;
    public TimeSpan TotalAccumulatedDuration { get; set; } = TimeSpan.Zero;
    public TimeSpan RemainingTime { get; set; } = TimeSpan.Zero;
    public bool IsCountdown { get; set; }
    public bool HasReachedBudget { get; set; }

    public string FormattedCurrentElapsed =>
        $"{(int)ElapsedCurrentSession.TotalHours:D2}:{ElapsedCurrentSession.Minutes:D2}:{ElapsedCurrentSession.Seconds:D2}";

    public string FormattedTotalAccumulated =>
        $"{(int)TotalAccumulatedDuration.TotalHours:D2}:{TotalAccumulatedDuration.Minutes:D2}:{TotalAccumulatedDuration.Seconds:D2}";

    public string FormattedRemainingTime =>
        $"{(int)RemainingTime.TotalHours:D2}:{RemainingTime.Minutes:D2}:{RemainingTime.Seconds:D2}";
}
