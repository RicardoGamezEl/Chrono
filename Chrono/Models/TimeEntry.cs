using System;
using SQLite;

namespace Chrono.Models;

public class TimeEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int TaskId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }

    [Ignore]
    public TimeSpan Duration
    {
        get
        {
            if (End.HasValue)
            {
                return End.Value >= Start ? End.Value - Start : TimeSpan.Zero;
            }
            return DateTime.Now >= Start ? DateTime.Now - Start : TimeSpan.Zero;
        }
    }

    [Ignore]
    public string FormattedDuration
    {
        get
        {
            var d = Duration;
            return $"{(int)d.TotalHours:D2}:{d.Minutes:D2}:{d.Seconds:D2}";
        }
    }

    [Ignore]
    public string FormattedPeriod
    {
        get
        {
            var endStr = End.HasValue ? End.Value.ToString("HH:mm:ss") : "En curso...";
            return $"{Start:dd/MM/yyyy HH:mm:ss} — {endStr}";
        }
    }

    [Ignore]
    public bool IsActive => !End.HasValue;
}
