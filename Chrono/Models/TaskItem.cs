using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace Chrono.Models;

public class TaskItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private int _projectId;
    private string _title = string.Empty;
    private string _description = string.Empty;
    private TaskStatus _status = TaskStatus.NotStarted;
    private DateTime _createdAt = DateTime.Now;
    private long _plannedDurationTicks;
    private TimeSpan _totalTime = TimeSpan.Zero;
    private bool _hasActiveSession;

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public int ProjectId
    {
        get => _projectId;
        set
        {
            if (_projectId != value)
            {
                _projectId = value;
                OnPropertyChanged();
            }
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                OnPropertyChanged();
            }
        }
    }

    public string Description
    {
        get => _description;
        set
        {
            if (_description != value)
            {
                _description = value;
                OnPropertyChanged();
            }
        }
    }

    public TaskStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (_createdAt != value)
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Planned duration stored as ticks for SQLite compatibility.
    /// Use the PlannedDuration property for TimeSpan access.
    /// </summary>
    public long PlannedDurationTicks
    {
        get => _plannedDurationTicks;
        set
        {
            if (_plannedDurationTicks != value)
            {
                _plannedDurationTicks = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PlannedDuration));
                OnPropertyChanged(nameof(FormattedPlannedDuration));
                OnPropertyChanged(nameof(HasPlannedDuration));
                OnPropertyChanged(nameof(RemainingTime));
                OnPropertyChanged(nameof(FormattedRemainingTime));
                OnPropertyChanged(nameof(ProgressRatio));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(FormattedProgressPercentage));
            }
        }
    }

    /// <summary>
    /// Planned duration as a TimeSpan. Reading/writing this also updates PlannedDurationTicks.
    /// </summary>
    [Ignore]
    public TimeSpan PlannedDuration
    {
        get => TimeSpan.FromTicks(_plannedDurationTicks);
        set => PlannedDurationTicks = value.Ticks;
    }

    [Ignore]
    public bool HasPlannedDuration => _plannedDurationTicks > 0;

    [Ignore]
    public string FormattedPlannedDuration
    {
        get
        {
            var d = PlannedDuration;
            return $"{(int)d.TotalHours:D2}:{d.Minutes:D2}:{d.Seconds:D2}";
        }
    }

    /// <summary>
    /// Remaining budget = PlannedDuration - TotalTime. Returns Zero if no planned duration or budget exhausted.
    /// </summary>
    [Ignore]
    public TimeSpan RemainingTime
    {
        get
        {
            if (!HasPlannedDuration)
                return TimeSpan.Zero;
            var remaining = PlannedDuration - TotalTime;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    [Ignore]
    public string FormattedRemainingTime
    {
        get
        {
            var r = RemainingTime;
            return $"{(int)r.TotalHours:D2}:{r.Minutes:D2}:{r.Seconds:D2}";
        }
    }

    [Ignore]
    public TimeSpan TotalTime
    {
        get => _totalTime;
        set
        {
            if (_totalTime != value)
            {
                _totalTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTotalTime));
                OnPropertyChanged(nameof(RemainingTime));
                OnPropertyChanged(nameof(FormattedRemainingTime));
                OnPropertyChanged(nameof(ProgressRatio));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(FormattedProgressPercentage));
            }
        }
    }

    [Ignore]
    public string FormattedTotalTime => $"{(int)TotalTime.TotalHours:D2}:{TotalTime.Minutes:D2}:{TotalTime.Seconds:D2}";

    [Ignore]
    public double ProgressRatio
    {
        get
        {
            if (!HasPlannedDuration || PlannedDurationTicks <= 0)
                return 0.0;
            var ratio = TotalTime.TotalSeconds / PlannedDuration.TotalSeconds;
            return Math.Min(1.0, Math.Max(0.0, ratio));
        }
    }

    [Ignore]
    public double ProgressPercentage => ProgressRatio * 100.0;

    [Ignore]
    public string FormattedProgressPercentage => $"{ProgressPercentage:F0}%";

    [Ignore]
    public bool HasActiveSession
    {
        get => _hasActiveSession;
        set
        {
            if (_hasActiveSession != value)
            {
                _hasActiveSession = value;
                OnPropertyChanged();
            }
        }
    }

    [Ignore]
    public string StatusDisplay => Status switch
    {
        TaskStatus.NotStarted => "Sin Iniciar",
        TaskStatus.InProgress => "En Progreso",
        TaskStatus.Completed => "Completado",
        _ => Status.ToString()
    };

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
