using SQLite;
using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace Chrono.Models;

public class TimeEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ForeignKey(nameof(TaskItem.Id))]
    public int TaskId { get; set; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }

    [Ignore]
    public TimeSpan Duration => End.HasValue ? End.Value - Start : DateTime.Now - Start;
}
