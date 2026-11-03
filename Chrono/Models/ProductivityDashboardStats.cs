using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chrono.Models;

public class ProductivityDashboardStats : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private TimeSpan _totalLoggedTime = TimeSpan.Zero;
    private TimeSpan _todayLoggedTime = TimeSpan.Zero;
    private TimeSpan _weekLoggedTime = TimeSpan.Zero;
    private TimeSpan _totalPlannedTime = TimeSpan.Zero;
    private TimeSpan _totalRemainingTime = TimeSpan.Zero;
    private TimeSpan _totalWorkedOnPlannedTime = TimeSpan.Zero;
    private double _timeProgressRatio;
    private double _timeProgressPercentage;
    private int _totalProjects;
    private int _totalTasks;
    private int _completedTasks;
    private int _inProgressTasks;
    private int _pendingTasks;
    private double _globalProgressRatio;
    private double _globalProgressPercentage;
    private RecentActivityItem? _activeSession;

    public TimeSpan TotalLoggedTime
    {
        get => _totalLoggedTime;
        set
        {
            if (_totalLoggedTime != value)
            {
                _totalLoggedTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTotalTime));
            }
        }
    }

    public string FormattedTotalTime =>
        $"{(int)TotalLoggedTime.TotalHours:D2}:{TotalLoggedTime.Minutes:D2}:{TotalLoggedTime.Seconds:D2}";

    public TimeSpan TodayLoggedTime
    {
        get => _todayLoggedTime;
        set
        {
            if (_todayLoggedTime != value)
            {
                _todayLoggedTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTodayTime));
            }
        }
    }

    public string FormattedTodayTime =>
        $"{(int)TodayLoggedTime.TotalHours:D2}:{TodayLoggedTime.Minutes:D2}:{TodayLoggedTime.Seconds:D2}";

    public TimeSpan WeekLoggedTime
    {
        get => _weekLoggedTime;
        set
        {
            if (_weekLoggedTime != value)
            {
                _weekLoggedTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedWeekTime));
            }
        }
    }

    public string FormattedWeekTime =>
        $"{(int)WeekLoggedTime.TotalHours:D2}:{WeekLoggedTime.Minutes:D2}:{WeekLoggedTime.Seconds:D2}";

    public TimeSpan TotalPlannedTime
    {
        get => _totalPlannedTime;
        set
        {
            if (_totalPlannedTime != value)
            {
                _totalPlannedTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTotalPlannedTime));
            }
        }
    }

    public string FormattedTotalPlannedTime =>
        $"{(int)TotalPlannedTime.TotalHours:D2}:{TotalPlannedTime.Minutes:D2}:{TotalPlannedTime.Seconds:D2}";

    public TimeSpan TotalRemainingTime
    {
        get => _totalRemainingTime;
        set
        {
            if (_totalRemainingTime != value)
            {
                _totalRemainingTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTotalRemainingTime));
            }
        }
    }

    public string FormattedTotalRemainingTime =>
        $"{(int)TotalRemainingTime.TotalHours:D2}:{TotalRemainingTime.Minutes:D2}:{TotalRemainingTime.Seconds:D2}";

    public TimeSpan TotalWorkedOnPlannedTime
    {
        get => _totalWorkedOnPlannedTime;
        set
        {
            if (_totalWorkedOnPlannedTime != value)
            {
                _totalWorkedOnPlannedTime = value;
                OnPropertyChanged();
            }
        }
    }

    public double TimeProgressRatio
    {
        get => _timeProgressRatio;
        set
        {
            if (Math.Abs(_timeProgressRatio - value) > 0.0001)
            {
                _timeProgressRatio = value;
                _timeProgressPercentage = value * 100.0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimeProgressPercentage));
                OnPropertyChanged(nameof(FormattedTimeProgress));
            }
        }
    }

    public double TimeProgressPercentage => _timeProgressPercentage;

    public string FormattedTimeProgress => $"{TimeProgressPercentage:F0}%";

    public int TotalProjects
    {
        get => _totalProjects;
        set
        {
            if (_totalProjects != value)
            {
                _totalProjects = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProjects));
            }
        }
    }

    public int TotalTasks
    {
        get => _totalTasks;
        set
        {
            if (_totalTasks != value)
            {
                _totalTasks = value;
                OnPropertyChanged();
            }
        }
    }

    public int CompletedTasks
    {
        get => _completedTasks;
        set
        {
            if (_completedTasks != value)
            {
                _completedTasks = value;
                OnPropertyChanged();
            }
        }
    }

    public int InProgressTasks
    {
        get => _inProgressTasks;
        set
        {
            if (_inProgressTasks != value)
            {
                _inProgressTasks = value;
                OnPropertyChanged();
            }
        }
    }

    public int PendingTasks
    {
        get => _pendingTasks;
        set
        {
            if (_pendingTasks != value)
            {
                _pendingTasks = value;
                OnPropertyChanged();
            }
        }
    }

    public double GlobalProgressRatio
    {
        get => _globalProgressRatio;
        set
        {
            if (Math.Abs(_globalProgressRatio - value) > 0.0001)
            {
                _globalProgressRatio = value;
                OnPropertyChanged();
            }
        }
    }

    public double GlobalProgressPercentage
    {
        get => _globalProgressPercentage;
        set
        {
            if (Math.Abs(_globalProgressPercentage - value) > 0.0001)
            {
                _globalProgressPercentage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedGlobalProgress));
            }
        }
    }

    public string FormattedGlobalProgress => $"{GlobalProgressPercentage:F0}%";

    public RecentActivityItem? ActiveSession
    {
        get => _activeSession;
        set
        {
            if (_activeSession != value)
            {
                _activeSession = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasActiveSession));
            }
        }
    }

    public bool HasActiveSession => ActiveSession != null;

    public List<ProjectProgressStats> Projects { get; set; } = new();
    public List<RecentActivityItem> RecentActivities { get; set; } = new();

    public bool HasProjects => TotalProjects > 0;
    public bool HasActivities => RecentActivities.Count > 0;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
