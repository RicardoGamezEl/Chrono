using SQLite;
using System;
namespace Chrono.Models;

public class TimeEntry
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    [Ignore]
    public TimeSpan Duration => End.HasValue ? End.Value - Start : DateTime.Now - Start;
}
