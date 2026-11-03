using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Chrono.Models;

public class RecentActivityItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private TimeSpan _duration = TimeSpan.Zero;
    private TimeSpan _remainingTime = TimeSpan.Zero;
    private bool _hasPlannedDuration;
    private DateTime? _end;

    public int TimeEntryId { get; set; }
    public int TaskId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime Start { get; set; }

    public DateTime? End
    {
        get => _end;
        set
        {
            if (_end != value)
            {
                _end = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsActive));
            }
        }
    }

    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration != value)
            {
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedDuration));
            }
        }
    }

    public string FormattedDuration =>
        $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";

    public bool HasPlannedDuration
    {
        get => _hasPlannedDuration;
        set
        {
            if (_hasPlannedDuration != value)
            {
                _hasPlannedDuration = value;
                OnPropertyChanged();
            }
        }
    }

    public TimeSpan RemainingTime
    {
        get => _remainingTime;
        set
        {
            if (_remainingTime != value)
            {
                _remainingTime = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedRemainingTime));
            }
        }
    }

    public string FormattedRemainingTime =>
        $"{(int)RemainingTime.TotalHours:D2}:{RemainingTime.Minutes:D2}:{RemainingTime.Seconds:D2}";

    public string FormattedDate => Start.Date == DateTime.Today
        ? $"Hoy {Start:HH:mm}"
        : Start.Date == DateTime.Today.AddDays(-1)
            ? $"Ayer {Start:HH:mm}"
            : $"{Start:dd/MM/yyyy HH:mm}";

    public bool IsActive => !End.HasValue;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
