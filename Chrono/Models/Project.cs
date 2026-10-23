using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SQLite;

namespace Chrono.Models;

public class Project : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _name = string.Empty;
    private string _description = string.Empty;
    private DateTime _createdAt = DateTime.Now;
    private TimeSpan _totalTime = TimeSpan.Zero;

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
