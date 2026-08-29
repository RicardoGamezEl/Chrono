using System;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite;

namespace Chrono.Models;
public class TaskItem
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [ForeignKey(nameof(Project.Id))]
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status {  get; set; } = TaskStatus.NotStarted;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
