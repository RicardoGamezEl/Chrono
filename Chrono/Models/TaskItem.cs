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
            }
        }
    }

    [Ignore]
    public string FormattedTotalTime => $"{(int)TotalTime.TotalHours:D2}:{TotalTime.Minutes:D2}:{TotalTime.Seconds:D2}";

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
