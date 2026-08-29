using System;

namespace Chrono.Models;
public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TaskStatus Status {  get; set; } = TaskStatus.NotStarted;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
