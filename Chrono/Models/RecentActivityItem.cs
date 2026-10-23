using System;

namespace Chrono.Models;

public class RecentActivityItem
{
    public int TimeEntryId { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan Duration { get; set; }
    public string FormattedDuration => $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";

    public string FormattedDate => Start.Date == DateTime.Today
        ? $"Hoy {Start:HH:mm}"
        : Start.Date == DateTime.Today.AddDays(-1)
            ? $"Ayer {Start:HH:mm}"
            : $"{Start:dd/MM/yyyy HH:mm}";

    public bool IsActive => !End.HasValue;
}
