using System;
using System.Collections.Generic;
using Chrono.Models;
using Chrono.Services;
using Xunit;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Tests;

public class ProductivityStatsCalculatorTests
{
    private readonly ProductivityStatsCalculator _calculator = new();

    [Fact]
    public void CalculateDashboardStats_EmptyData_ReturnsZeroedMetricsWithoutException()
    {
        // Arrange
        var projects = new List<Project>();
        var tasks = new List<TaskItem>();
        var timeEntries = new List<TimeEntry>();

        // Act
        var result = _calculator.CalculateDashboardStats(projects, tasks, timeEntries);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalProjects);
        Assert.Equal(0, result.TotalTasks);
        Assert.Equal(0, result.CompletedTasks);
        Assert.Equal(0, result.InProgressTasks);
        Assert.Equal(0, result.PendingTasks);
        Assert.Equal(0.0, result.GlobalProgressRatio);
        Assert.Equal(0.0, result.GlobalProgressPercentage);
        Assert.Equal("0%", result.FormattedGlobalProgress);
        Assert.Equal(TimeSpan.Zero, result.TotalLoggedTime);
        Assert.Equal(TimeSpan.Zero, result.TodayLoggedTime);
        Assert.Equal(TimeSpan.Zero, result.WeekLoggedTime);
        Assert.Empty(result.Projects);
        Assert.Empty(result.RecentActivities);
        Assert.False(result.HasProjects);
        Assert.False(result.HasActivities);
    }

    [Fact]
    public void CalculateDashboardStats_TasksProgress_CalculatesCorrectPercentages()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "Proyecto Test" };
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, ProjectId = 1, Title = "T1", Status = TaskStatus.Completed },
            new() { Id = 2, ProjectId = 1, Title = "T2", Status = TaskStatus.Completed },
            new() { Id = 3, ProjectId = 1, Title = "T3", Status = TaskStatus.InProgress },
            new() { Id = 4, ProjectId = 1, Title = "T4", Status = TaskStatus.NotStarted }
        };

        // Act
        var result = _calculator.CalculateDashboardStats(new[] { project }, tasks, Array.Empty<TimeEntry>());

        // Assert
        Assert.Equal(4, result.TotalTasks);
        Assert.Equal(2, result.CompletedTasks);
        Assert.Equal(1, result.InProgressTasks);
        Assert.Equal(1, result.PendingTasks);
        Assert.Equal(0.5, result.GlobalProgressRatio);
        Assert.Equal(50.0, result.GlobalProgressPercentage);
        Assert.Equal("50%", result.FormattedGlobalProgress);
    }

    [Fact]
    public void CalculateDashboardStats_TimeTracking_CalculatesTotalTodayAndWeekTimeCorrectly()
    {
        // Arrange: Wednesday Oct 21, 2026 as reference date
        var referenceNow = new DateTime(2026, 10, 21, 15, 0, 0);

        var entries = new List<TimeEntry>
        {
            // Today (2h)
            new() { Id = 1, TaskId = 1, Start = new DateTime(2026, 10, 21, 10, 0, 0), End = new DateTime(2026, 10, 21, 12, 0, 0) },
            // Earlier this week - Monday Oct 19 (3h)
            new() { Id = 2, TaskId = 1, Start = new DateTime(2026, 10, 19, 9, 0, 0), End = new DateTime(2026, 10, 19, 12, 0, 0) },
            // Previous week - Friday Oct 16 (5h)
            new() { Id = 3, TaskId = 1, Start = new DateTime(2026, 10, 16, 8, 0, 0), End = new DateTime(2026, 10, 16, 13, 0, 0) }
        };

        // Act
        var result = _calculator.CalculateDashboardStats(
            Array.Empty<Project>(),
            Array.Empty<TaskItem>(),
            entries,
            referenceNow);

        // Assert
        Assert.Equal(TimeSpan.FromHours(10), result.TotalLoggedTime);
        Assert.Equal(TimeSpan.FromHours(2), result.TodayLoggedTime);
        Assert.Equal(TimeSpan.FromHours(5), result.WeekLoggedTime);
        Assert.Equal("10:00:00", result.FormattedTotalTime);
        Assert.Equal("02:00:00", result.FormattedTodayTime);
        Assert.Equal("05:00:00", result.FormattedWeekTime);
    }

    [Fact]
    public void CalculateDashboardStats_ProjectBreakdown_CalculatesIndividualProjectStats()
    {
        // Arrange
        var p1 = new Project { Id = 1, Name = "Alpha" };
        var p2 = new Project { Id = 2, Name = "Beta" };

        var tasks = new List<TaskItem>
        {
            new() { Id = 10, ProjectId = 1, Status = TaskStatus.Completed },
            new() { Id = 11, ProjectId = 1, Status = TaskStatus.Completed },
            new() { Id = 12, ProjectId = 1, Status = TaskStatus.NotStarted },
            new() { Id = 20, ProjectId = 2, Status = TaskStatus.Completed }
        };

        var entries = new List<TimeEntry>
        {
            // 4h in Alpha
            new() { Id = 1, TaskId = 10, Start = new DateTime(2026, 1, 1, 10, 0, 0), End = new DateTime(2026, 1, 1, 14, 0, 0) },
            // 1h in Beta
            new() { Id = 2, TaskId = 20, Start = new DateTime(2026, 1, 1, 10, 0, 0), End = new DateTime(2026, 1, 1, 11, 0, 0) }
        };

        // Act
        var result = _calculator.CalculateDashboardStats(new[] { p1, p2 }, tasks, entries);

        // Assert
        Assert.Equal(2, result.Projects.Count);

        var alphaStats = result.Projects[0];
        Assert.Equal("Alpha", alphaStats.ProjectName);
        Assert.Equal(3, alphaStats.TotalTasks);
        Assert.Equal(2, alphaStats.CompletedTasks);
        Assert.Equal(TimeSpan.FromHours(4), alphaStats.TotalTime);
        Assert.Equal(2.0 / 3.0, alphaStats.ProgressRatio, precision: 3);
        Assert.Equal("67%", alphaStats.FormattedProgress);

        var betaStats = result.Projects[1];
        Assert.Equal("Beta", betaStats.ProjectName);
        Assert.Equal(1, betaStats.TotalTasks);
        Assert.Equal(1, betaStats.CompletedTasks);
        Assert.Equal(TimeSpan.FromHours(1), betaStats.TotalTime);
        Assert.Equal(1.0, betaStats.ProgressRatio);
        Assert.Equal("100%", betaStats.FormattedProgress);
    }

    [Fact]
    public void CalculateDashboardStats_RecentActivities_ReturnsTopChronologicalSessions()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "Mi Proyecto" };
        var task = new TaskItem { Id = 10, ProjectId = 1, Title = "Tarea Principal" };

        var entries = new List<TimeEntry>
        {
            new() { Id = 1, TaskId = 10, Start = new DateTime(2026, 10, 1, 10, 0, 0), End = new DateTime(2026, 10, 1, 11, 0, 0) },
            new() { Id = 2, TaskId = 10, Start = new DateTime(2026, 10, 3, 10, 0, 0), End = new DateTime(2026, 10, 3, 11, 0, 0) },
            new() { Id = 3, TaskId = 10, Start = new DateTime(2026, 10, 2, 10, 0, 0), End = new DateTime(2026, 10, 2, 11, 0, 0) }
        };

        // Act
        var result = _calculator.CalculateDashboardStats(new[] { project }, new[] { task }, entries);

        // Assert
        Assert.Equal(3, result.RecentActivities.Count);
        Assert.Equal(2, result.RecentActivities[0].TimeEntryId); // Oct 3
        Assert.Equal(3, result.RecentActivities[1].TimeEntryId); // Oct 2
        Assert.Equal(1, result.RecentActivities[2].TimeEntryId); // Oct 1
        Assert.Equal("Tarea Principal", result.RecentActivities[0].TaskTitle);
        Assert.Equal("Mi Proyecto", result.RecentActivities[0].ProjectName);
    }

    [Fact]
    public void CalculateTotalDuration_Over24Hours_CalculatesExactSpan()
    {
        // Arrange
        var entries = new List<TimeEntry>
        {
            new() { Id = 1, TaskId = 1, Start = new DateTime(2026, 1, 1, 0, 0, 0), End = new DateTime(2026, 1, 2, 6, 0, 0) } // 30 hours
        };

        // Act
        var total = _calculator.CalculateTotalDuration(entries);

        // Assert
        Assert.Equal(30, total.TotalHours);
        Assert.Equal("30:00:00", $"{(int)total.TotalHours:D2}:{total.Minutes:D2}:{total.Seconds:D2}");
    }

    [Fact]
    public void CalculateDashboardStats_PlannedAndRemainingTime_CalculatesCorrectBudgetAndProgress()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "P1" };
        var tasks = new List<TaskItem>
        {
            // Task 1: 2h planned, 30m worked -> 1h 30m remaining
            new() { Id = 1, ProjectId = 1, Title = "T1", PlannedDurationTicks = TimeSpan.FromHours(2).Ticks },
            // Task 2: 1h planned, 45m worked -> 15m remaining
            new() { Id = 2, ProjectId = 1, Title = "T2", PlannedDurationTicks = TimeSpan.FromHours(1).Ticks },
            // Task 3: 0h planned (stopwatch) -> 20m worked -> 0 remaining
            new() { Id = 3, ProjectId = 1, Title = "T3", PlannedDurationTicks = 0 }
        };

        var entries = new List<TimeEntry>
        {
            new() { Id = 1, TaskId = 1, Start = new DateTime(2026, 1, 1, 10, 0, 0), End = new DateTime(2026, 1, 1, 10, 30, 0) },
            new() { Id = 2, TaskId = 2, Start = new DateTime(2026, 1, 1, 11, 0, 0), End = new DateTime(2026, 1, 1, 11, 45, 0) },
            new() { Id = 3, TaskId = 3, Start = new DateTime(2026, 1, 1, 12, 0, 0), End = new DateTime(2026, 1, 1, 12, 20, 0) }
        };

        // Act
        var result = _calculator.CalculateDashboardStats(new[] { project }, tasks, entries);

        // Assert
        // Total Planned = 2h + 1h = 3h
        Assert.Equal(TimeSpan.FromHours(3), result.TotalPlannedTime);
        Assert.Equal("03:00:00", result.FormattedTotalPlannedTime);

        // Total Remaining = 1h 30m + 15m = 1h 45m
        Assert.Equal(TimeSpan.FromMinutes(105), result.TotalRemainingTime);
        Assert.Equal("01:45:00", result.FormattedTotalRemainingTime);

        // Total Worked on planned tasks = 30m + 45m = 75m
        // Total Planned = 180m
        // Progress Ratio = 75 / 180 = 0.416666...
        Assert.Equal(75.0 / 180.0, result.TimeProgressRatio, precision: 3);
        Assert.Equal("42%", result.FormattedTimeProgress);

        // Total Logged Time across all tasks (including T3) = 30m + 45m + 20m = 95m (1h 35m)
        Assert.Equal(TimeSpan.FromMinutes(95), result.TotalLoggedTime);
        Assert.Equal("01:35:00", result.FormattedTotalTime);
    }

    [Fact]
    public void CalculateDashboardStats_ActiveSession_DetectedCorrectly()
    {
        // Arrange
        var project = new Project { Id = 1, Name = "Proyecto Activo" };
        var task = new TaskItem { Id = 10, ProjectId = 1, Title = "Tarea en Curso" };

        var entries = new List<TimeEntry>
        {
            // Completed session
            new() { Id = 1, TaskId = 10, Start = new DateTime(2026, 1, 1, 9, 0, 0), End = new DateTime(2026, 1, 1, 10, 0, 0) },
            // Ongoing session (End is null)
            new() { Id = 2, TaskId = 10, Start = new DateTime(2026, 1, 1, 11, 0, 0), End = null }
        };

        // Act
        var result = _calculator.CalculateDashboardStats(new[] { project }, new[] { task }, entries);

        // Assert
        Assert.True(result.HasActiveSession);
        Assert.NotNull(result.ActiveSession);
        Assert.Equal(2, result.ActiveSession.TimeEntryId);
        Assert.Equal("Tarea en Curso", result.ActiveSession.TaskTitle);
        Assert.Equal("Proyecto Activo", result.ActiveSession.ProjectName);
        Assert.Null(result.ActiveSession.End);
    }
}
