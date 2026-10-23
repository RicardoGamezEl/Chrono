using System;
using System.Collections.Generic;
using System.Linq;
using Chrono.Models;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Services;

public class ProductivityStatsCalculator
{
    public ProductivityDashboardStats CalculateDashboardStats(
        IReadOnlyList<Project> projects,
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyList<TimeEntry> timeEntries,
        DateTime? referenceNow = null)
    {
        var now = referenceNow ?? DateTime.Now;
        var today = now.Date;
        var startOfWeek = GetStartOfWeek(today);

        var stats = new ProductivityDashboardStats
        {
            TotalProjects = projects.Count,
            TotalTasks = tasks.Count,
            CompletedTasks = tasks.Count(t => t.Status == TaskStatus.Completed),
            InProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress),
            PendingTasks = tasks.Count(t => t.Status == TaskStatus.NotStarted)
        };

        // Progress ratios
        if (stats.TotalTasks > 0)
        {
            stats.GlobalProgressRatio = (double)stats.CompletedTasks / stats.TotalTasks;
            stats.GlobalProgressPercentage = stats.GlobalProgressRatio * 100.0;
        }
        else
        {
            stats.GlobalProgressRatio = 0.0;
            stats.GlobalProgressPercentage = 0.0;
        }

        // Time calculations
        stats.TotalLoggedTime = CalculateTotalDuration(timeEntries);
        stats.TodayLoggedTime = CalculateDurationForDate(timeEntries, today);
        stats.WeekLoggedTime = CalculateDurationForDateRange(timeEntries, startOfWeek, today.AddDays(1).AddTicks(-1));

        // Projects breakdown
        var tasksByProject = tasks.GroupBy(t => t.ProjectId).ToDictionary(g => g.Key, g => g.ToList());
        var tasksMap = tasks.ToDictionary(t => t.Id);
        var projectsMap = projects.ToDictionary(p => p.Id);

        var projectStatsList = new List<ProjectProgressStats>();
        foreach (var project in projects)
        {
            tasksByProject.TryGetValue(project.Id, out var projectTasks);
            projectTasks ??= new List<TaskItem>();

            var projectTaskIds = projectTasks.Select(t => t.Id).ToHashSet();
            var projectEntries = timeEntries.Where(te => projectTaskIds.Contains(te.TaskId)).ToList();

            var projectStats = new ProjectProgressStats
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectDescription = project.Description,
                TotalTasks = projectTasks.Count,
                CompletedTasks = projectTasks.Count(t => t.Status == TaskStatus.Completed),
                InProgressTasks = projectTasks.Count(t => t.Status == TaskStatus.InProgress),
                NotStartedTasks = projectTasks.Count(t => t.Status == TaskStatus.NotStarted),
                TotalTime = CalculateTotalDuration(projectEntries)
            };

            if (projectStats.TotalTasks > 0)
            {
                projectStats.ProgressRatio = (double)projectStats.CompletedTasks / projectStats.TotalTasks;
                projectStats.ProgressPercentage = projectStats.ProgressRatio * 100.0;
            }
            else
            {
                projectStats.ProgressRatio = 0.0;
                projectStats.ProgressPercentage = 0.0;
            }

            projectStatsList.Add(projectStats);
        }

        // Sort projects by total time descending
        stats.Projects = projectStatsList.OrderByDescending(p => p.TotalTime).ToList();

        // Recent activity items (up to 5 most recent)
        stats.RecentActivities = GetRecentActivities(timeEntries, tasksMap, projectsMap, limit: 5);

        return stats;
    }

    public TimeSpan CalculateTotalDuration(IEnumerable<TimeEntry> entries)
    {
        var total = TimeSpan.Zero;
        foreach (var entry in entries)
        {
            total += entry.Duration;
        }
        return total;
    }

    public TimeSpan CalculateDurationForDate(IEnumerable<TimeEntry> entries, DateTime date)
    {
        var targetDate = date.Date;
        var total = TimeSpan.Zero;
        foreach (var entry in entries.Where(te => te.Start.Date == targetDate))
        {
            total += entry.Duration;
        }
        return total;
    }

    public TimeSpan CalculateDurationForDateRange(IEnumerable<TimeEntry> entries, DateTime start, DateTime end)
    {
        var total = TimeSpan.Zero;
        foreach (var entry in entries.Where(te => te.Start >= start && te.Start <= end))
        {
            total += entry.Duration;
        }
        return total;
    }

    public DateTime GetStartOfWeek(DateTime date)
    {
        // Monday as first day of week
        int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-1 * diff);
    }

    public List<RecentActivityItem> GetRecentActivities(
        IEnumerable<TimeEntry> entries,
        IReadOnlyDictionary<int, TaskItem> tasksMap,
        IReadOnlyDictionary<int, Project> projectsMap,
        int limit = 5)
    {
        return entries
            .OrderByDescending(te => te.Start)
            .Take(limit)
            .Select(te =>
            {
                var hasTask = tasksMap.TryGetValue(te.TaskId, out var task);
                var taskTitle = hasTask && task != null ? task.Title : $"Tarea #{te.TaskId}";
                var projectId = hasTask && task != null ? task.ProjectId : 0;
                var hasProject = projectsMap.TryGetValue(projectId, out var project);
                var projectName = hasProject && project != null ? project.Name : "Proyecto";

                return new RecentActivityItem
                {
                    TimeEntryId = te.Id,
                    TaskId = te.TaskId,
                    TaskTitle = taskTitle,
                    ProjectId = projectId,
                    ProjectName = projectName,
                    Start = te.Start,
                    End = te.End,
                    Duration = te.Duration
                };
            })
            .ToList();
    }
}
