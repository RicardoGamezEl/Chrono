using System;
using System.IO;
using System.Threading.Tasks;
using Chrono.Models;
using Chrono.Services;
using TaskStatus = Chrono.Models.TaskStatus;
using Xunit;

namespace Chrono.Tests;

public class TestNotificationService : INotificationService
{
    public System.Collections.Generic.List<(string Title, string Message)> Notifications { get; } = new();
    public event Action<string, string>? NotificationReceived;

    public void ShowToast(string title, string message, int durationSeconds = 4)
    {
        Notifications.Add((title, message));
        NotificationReceived?.Invoke(title, message);
    }
}

public class TaskTimerServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DatabaseService _databaseService;
    private readonly TestNotificationService _notificationService;
    private readonly TaskTimerService _timerService;

    public TaskTimerServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"chrono_test_{Guid.NewGuid():N}.db");
        _databaseService = new DatabaseService(_dbPath);
        _databaseService.InitializeAsync().GetAwaiter().GetResult();

        _notificationService = new TestNotificationService();
        _timerService = new TaskTimerService(_databaseService, _notificationService, enableTicker: false);
    }

    public void Dispose()
    {
        _timerService.Dispose();
        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { }
        }
    }

    private async Task<(Project project, TaskItem task)> CreateProjectAndTaskAsync(TimeSpan plannedDuration)
    {
        var project = new Project { Name = "Proyecto Test" };
        await _databaseService.SaveProjectAsync(project);

        var task = new TaskItem
        {
            ProjectId = project.Id,
            Title = "Tarea Test",
            PlannedDurationTicks = plannedDuration.Ticks,
            Status = TaskStatus.NotStarted,
            CreatedAt = DateTime.Now
        };
        await _databaseService.SaveTaskAsync(task);

        return (project, task);
    }

    [Fact]
    public void RemainingTime_CalculatesCorrectly_AndNeverNegative()
    {
        var task = new TaskItem
        {
            Title = "Tarea Cálculo",
            PlannedDuration = TimeSpan.FromHours(2),
            TotalTime = TimeSpan.FromMinutes(30)
        };

        // 2h - 30m = 1h 30m
        Assert.Equal(TimeSpan.FromMinutes(90), task.RemainingTime);
        Assert.Equal("01:30:00", task.FormattedRemainingTime);

        // When worked equals planned: remaining is zero
        task.TotalTime = TimeSpan.FromHours(2);
        Assert.Equal(TimeSpan.Zero, task.RemainingTime);
        Assert.Equal("00:00:00", task.FormattedRemainingTime);

        // When worked exceeds planned: remaining is clamped to zero, never negative
        task.TotalTime = TimeSpan.FromHours(2).Add(TimeSpan.FromMinutes(15));
        Assert.Equal(TimeSpan.Zero, task.RemainingTime);
        Assert.True(task.RemainingTime >= TimeSpan.Zero);
        Assert.Equal("00:00:00", task.FormattedRemainingTime);
    }

    [Fact]
    public async Task MultipleSessions_TrackBudgetAcrossSessions_WithoutResetting()
    {
        // Budget: 02:00:00
        var (_, task) = await CreateProjectAndTaskAsync(TimeSpan.FromHours(2));

        var t0 = new DateTime(2026, 1, 1, 10, 0, 0);

        // Session 1: 00:30:00
        await _timerService.StartSessionAsync(task.Id, referenceStartTime: t0);
        var state1 = await _timerService.StopSessionAsync(task.Id, referenceEndTime: t0.AddMinutes(30));

        Assert.NotNull(state1);
        Assert.Equal(TimeSpan.FromMinutes(30), state1.ElapsedCurrentSession);
        Assert.Equal(TimeSpan.FromMinutes(30), state1.TotalAccumulatedDuration);
        Assert.Equal(TimeSpan.FromMinutes(90), state1.RemainingTime); // 01:30:00 remaining

        // Session 2: 00:45:00 starting later
        var t1 = new DateTime(2026, 1, 1, 14, 0, 0);
        await _timerService.StartSessionAsync(task.Id, referenceStartTime: t1);
        var state2 = await _timerService.StopSessionAsync(task.Id, referenceEndTime: t1.AddMinutes(45));

        Assert.NotNull(state2);
        Assert.Equal(TimeSpan.FromMinutes(45), state2.ElapsedCurrentSession);
        Assert.Equal(TimeSpan.FromMinutes(75), state2.TotalAccumulatedDuration); // 1h 15m total
        Assert.Equal(TimeSpan.FromMinutes(45), state2.RemainingTime); // 00:45:00 remaining
        Assert.False(state2.HasReachedBudget);
    }

    [Fact]
    public async Task EarlyStop_RecordsWorkedInterval_AndDoesNotMarkCompleted()
    {
        // Planned: 2 hours. User stops early after 20 minutes.
        var (_, task) = await CreateProjectAndTaskAsync(TimeSpan.FromHours(2));

        var start = new DateTime(2026, 1, 1, 9, 0, 0);
        await _timerService.StartSessionAsync(task.Id, referenceStartTime: start);

        var stoppedState = await _timerService.StopSessionAsync(task.Id, referenceEndTime: start.AddMinutes(20));

        Assert.NotNull(stoppedState);
        Assert.Equal(TimeSpan.FromMinutes(20), stoppedState.ElapsedCurrentSession);
        Assert.Equal(TimeSpan.FromMinutes(100), stoppedState.RemainingTime); // 1h 40m remaining
        Assert.False(stoppedState.HasReachedBudget);

        // Verify task in DB is not completed automatically
        var dbTask = await _databaseService.GetTaskByIdAsync(task.Id);
        Assert.NotNull(dbTask);
        Assert.NotEqual(TaskStatus.Completed, dbTask.Status);
        Assert.Equal(TaskStatus.InProgress, dbTask.Status);
    }

    [Fact]
    public async Task AutomaticCompletion_CapsWorkedIntervalToRemainingBudget_AndShowsNotification()
    {
        // Planned: 1 hour. Prior session: 45 min. Remaining: 15 min.
        var (_, task) = await CreateProjectAndTaskAsync(TimeSpan.FromHours(1));

        // Prior session of 45 mins
        var priorEntry = new TimeEntry
        {
            TaskId = task.Id,
            Start = new DateTime(2026, 1, 1, 8, 0, 0),
            End = new DateTime(2026, 1, 1, 8, 45, 0)
        };
        await _databaseService.SaveTimeEntryAsync(priorEntry);

        // Now start new session at 10:00:00. Remaining budget is 15 minutes.
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        await _timerService.StartSessionAsync(task.Id, referenceStartTime: start);

        // Stop at 10:30:00 (30 minutes elapsed, but only 15 minutes were remaining!)
        var state = await _timerService.StopSessionAsync(task.Id, referenceEndTime: start.AddMinutes(30));

        Assert.NotNull(state);
        Assert.True(state.HasReachedBudget);
        // Recorded worked interval must be capped to remaining budget (15 mins), not 30 mins!
        Assert.Equal(TimeSpan.FromMinutes(15), state.ElapsedCurrentSession);
        Assert.Equal(TimeSpan.FromHours(1), state.TotalAccumulatedDuration);
        Assert.Equal(TimeSpan.Zero, state.RemainingTime);

        // Notification must have been sent
        Assert.Contains(_notificationService.Notifications, n => n.Title == "Tiempo cumplido");

        // Task status should not automatically be Completed
        var dbTask = await _databaseService.GetTaskByIdAsync(task.Id);
        Assert.NotNull(dbTask);
        Assert.NotEqual(TaskStatus.Completed, dbTask.Status);
    }

    [Fact]
    public async Task ReconcileExpiredSession_ReconcilesCorrectly_WhenAppReopened()
    {
        // Planned budget: 1 hour.
        var (_, task) = await CreateProjectAndTaskAsync(TimeSpan.FromHours(1));

        // Session started at 10:00:00.
        var start = new DateTime(2026, 1, 1, 10, 0, 0);
        await _timerService.StartSessionAsync(task.Id, referenceStartTime: start);

        // App was closed/backgrounded, and reopened at 12:00:00 (2 hours later).
        var resumeTime = start.AddHours(2);
        var reconciledState = await _timerService.ReconcileActiveSessionAsync(referenceNow: resumeTime);

        Assert.NotNull(reconciledState);
        Assert.True(reconciledState.HasReachedBudget);
        // Entry must be finalized with exactly 1 hour duration (end = start + 1h = 11:00:00)
        Assert.Equal(TimeSpan.FromHours(1), reconciledState.ElapsedCurrentSession);
        Assert.Equal(start.AddHours(1), reconciledState.Entry.End);
        Assert.Equal(TimeSpan.Zero, reconciledState.RemainingTime);

        // Active session in DB should now be null (closed)
        var activeInDb = await _databaseService.GetActiveTimeEntryAsync();
        Assert.Null(activeInDb);

        // Notification must have been fired
        Assert.Contains(_notificationService.Notifications, n => n.Title == "Tiempo cumplido");
    }

    [Fact]
    public async Task PreventDuplicateSessions_RejectsAnotherTask_AndIsIdempotentForSameTask()
    {
        var (_, task1) = await CreateProjectAndTaskAsync(TimeSpan.FromHours(1));
        var (_, task2) = await CreateProjectAndTaskAsync(TimeSpan.FromHours(2));

        // Start session on Task 1
        var state1 = await _timerService.StartSessionAsync(task1.Id);
        Assert.NotNull(state1);

        // Starting another session on Task 2 must be prevented
        await Assert.ThrowsAsync<InvalidOperationException>(() => _timerService.StartSessionAsync(task2.Id));

        // Starting again on Task 1 must be idempotent and return existing session
        var stateAgain = await _timerService.StartSessionAsync(task1.Id);
        Assert.NotNull(stateAgain);
        Assert.Equal(state1.Entry.Id, stateAgain.Entry.Id);

        // Clean up
        await _timerService.StopSessionAsync(task1.Id);
    }

    [Fact]
    public async Task ZeroPlannedDuration_WorksAsProgressiveStopwatch()
    {
        // Legacy or zero-planned task: PlannedDuration = TimeSpan.Zero
        var (_, task) = await CreateProjectAndTaskAsync(TimeSpan.Zero);

        Assert.False(task.HasPlannedDuration);
        Assert.Equal(TimeSpan.Zero, task.RemainingTime);

        var start = new DateTime(2026, 1, 1, 8, 0, 0);
        var startState = await _timerService.StartSessionAsync(task.Id, referenceStartTime: start);

        Assert.False(startState.IsCountdown);

        var stopState = await _timerService.StopSessionAsync(task.Id, referenceEndTime: start.AddMinutes(42));

        Assert.NotNull(stopState);
        Assert.False(stopState.IsCountdown);
        Assert.False(stopState.HasReachedBudget);
        Assert.Equal(TimeSpan.FromMinutes(42), stopState.ElapsedCurrentSession);
        Assert.Equal(TimeSpan.FromMinutes(42), stopState.TotalAccumulatedDuration);
    }

    [Fact]
    public void LargeDurations_GreaterThan24Hours_HandledAccurately()
    {
        // Task with 36 hours planned duration
        var task = new TaskItem
        {
            Title = "Proyecto Extenso",
            PlannedDuration = TimeSpan.FromHours(36),
            TotalTime = TimeSpan.FromHours(10)
        };

        Assert.Equal("36:00:00", task.FormattedPlannedDuration);
        Assert.Equal(TimeSpan.FromHours(26), task.RemainingTime);
        Assert.Equal("26:00:00", task.FormattedRemainingTime);

        // Even 100 hours
        task.PlannedDuration = TimeSpan.FromHours(100);
        task.TotalTime = TimeSpan.FromHours(25);
        Assert.Equal("100:00:00", task.FormattedPlannedDuration);
        Assert.Equal("75:00:00", task.FormattedRemainingTime);
    }

    [Fact]
    public async Task LegacyDatabaseMigration_PreservesExistingTasks_AndDefaultsPlannedDurationToZero()
    {
        var migrationDbPath = Path.Combine(Path.GetTempPath(), $"chrono_migr_{Guid.NewGuid():N}.db");
        try
        {
            // 1. Simulate legacy database without PlannedDurationTicks column
            var directConn = new SQLite.SQLiteConnection(migrationDbPath);
            directConn.Execute(@"
                CREATE TABLE Project (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT,
                    Description TEXT,
                    CreatedAt DATETIME
                );
            ");
            directConn.Execute(@"
                CREATE TABLE TaskItem (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProjectId INTEGER,
                    Title TEXT,
                    Description TEXT,
                    Status INTEGER,
                    CreatedAt DATETIME
                );
            ");
            directConn.Execute(@"
                CREATE TABLE TimeEntry (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TaskId INTEGER,
                    Start DATETIME,
                    End DATETIME
                );
            ");

            // Insert legacy project, task, and session (using ticks as sqlite-net-pcl stores DateTime)
            var nowTicks = DateTime.Now.Ticks;
            directConn.Execute("INSERT INTO Project (Name, Description, CreatedAt) VALUES ('Proyecto Antiguo', 'Desc', ?);", nowTicks);
            directConn.Execute("INSERT INTO TaskItem (ProjectId, Title, Description, Status, CreatedAt) VALUES (1, 'Tarea Antigua', 'Desc', 0, ?);", nowTicks);
            var sessionStart = new DateTime(2025, 1, 1, 10, 0, 0);
            var sessionEnd = new DateTime(2025, 1, 1, 10, 30, 0);
            directConn.Execute("INSERT INTO TimeEntry (TaskId, Start, End) VALUES (1, ?, ?);", sessionStart.Ticks, sessionEnd.Ticks);
            directConn.Close();

            // 2. Open with Chrono DatabaseService and initialize
            var legacyDbService = new DatabaseService(migrationDbPath);
            await legacyDbService.InitializeAsync();

            // Verify repeated initialization is safe and idempotent
            await legacyDbService.InitializeAsync();

            // 3. Verify legacy data is completely preserved
            var projects = await legacyDbService.GetProjectsAsync();
            Assert.Single(projects);
            Assert.Equal("Proyecto Antiguo", projects[0].Name);

            var tasks = await legacyDbService.GetAllTasksAsync();
            Assert.Single(tasks);
            var oldTask = tasks[0];
            Assert.Equal("Tarea Antigua", oldTask.Title);
            Assert.Equal(0, oldTask.PlannedDurationTicks);
            Assert.False(oldTask.HasPlannedDuration);
            Assert.Equal("00:00:00", oldTask.FormattedPlannedDuration);

            var entries = await legacyDbService.GetAllTimeEntriesAsync();
            Assert.Single(entries);
            Assert.Equal(TimeSpan.FromMinutes(30), entries[0].Duration);

            // 4. Verify old task can subsequently be updated with a planned duration
            oldTask.PlannedDuration = TimeSpan.FromHours(3);
            await legacyDbService.UpdateTaskAsync(oldTask);

            var updatedTask = await legacyDbService.GetTaskByIdAsync(oldTask.Id);
            Assert.NotNull(updatedTask);
            Assert.Equal(TimeSpan.FromHours(3).Ticks, updatedTask.PlannedDurationTicks);
            Assert.True(updatedTask.HasPlannedDuration);
            Assert.Equal("03:00:00", updatedTask.FormattedPlannedDuration);
        }
        finally
        {
            if (File.Exists(migrationDbPath))
            {
                try { File.Delete(migrationDbPath); } catch { }
            }
        }
    }
}
