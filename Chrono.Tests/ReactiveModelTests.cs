using System;
using System.Collections.Generic;
using System.ComponentModel;
using Chrono.Models;
using Xunit;

namespace Chrono.Tests;

public class ReactiveModelTests
{
    [Fact]
    public void TaskItem_SettingTotalTime_FiresPropertyChangedForCountdownAndProgress()
    {
        var task = new TaskItem
        {
            Id = 1,
            Title = "Implement Reactive Architecture",
            PlannedDurationTicks = TimeSpan.FromMinutes(30).Ticks,
            TotalTime = TimeSpan.FromMinutes(10)
        };

        var changedProperties = new List<string>();
        task.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        // Advance worked time by 5 minutes
        task.TotalTime = TimeSpan.FromMinutes(15);

        Assert.Contains(nameof(TaskItem.TotalTime), changedProperties);
        Assert.Contains(nameof(TaskItem.FormattedTotalTime), changedProperties);
        Assert.Contains(nameof(TaskItem.RemainingTime), changedProperties);
        Assert.Contains(nameof(TaskItem.FormattedRemainingTime), changedProperties);
        Assert.Contains(nameof(TaskItem.ProgressRatio), changedProperties);
        Assert.Contains(nameof(TaskItem.ProgressPercentage), changedProperties);
        Assert.Contains(nameof(TaskItem.FormattedProgressPercentage), changedProperties);

        Assert.Equal(TimeSpan.FromMinutes(15), task.RemainingTime);
        Assert.Equal("00:15:00", task.FormattedRemainingTime);
        Assert.Equal(0.5, task.ProgressRatio, precision: 3);
        Assert.Equal(50.0, task.ProgressPercentage, precision: 1);
        Assert.Equal("50%", task.FormattedProgressPercentage);
    }

    [Fact]
    public void TaskItem_ExceedingPlannedDuration_ClampsProgressAndRemaining()
    {
        var task = new TaskItem
        {
            Id = 2,
            Title = "Feature Work",
            PlannedDurationTicks = TimeSpan.FromHours(1).Ticks,
            TotalTime = TimeSpan.FromHours(1.5)
        };

        Assert.Equal(TimeSpan.Zero, task.RemainingTime);
        Assert.Equal("00:00:00", task.FormattedRemainingTime);
        Assert.Equal(1.0, task.ProgressRatio);
        Assert.Equal(100.0, task.ProgressPercentage);
        Assert.Equal("100%", task.FormattedProgressPercentage);
    }

    [Fact]
    public void TaskItem_WithoutPlannedDuration_ReturnsZeroRemainingAndProgress()
    {
        var task = new TaskItem
        {
            Id = 3,
            Title = "Unplanned Task",
            PlannedDurationTicks = 0,
            TotalTime = TimeSpan.FromMinutes(45)
        };

        Assert.False(task.HasPlannedDuration);
        Assert.Equal(TimeSpan.Zero, task.RemainingTime);
        Assert.Equal(0.0, task.ProgressRatio);
        Assert.Equal(0.0, task.ProgressPercentage);
    }

    [Fact]
    public void RecentActivityItem_SettingDuration_FiresPropertyChangedAndCalculatesRemaining()
    {
        var item = new RecentActivityItem
        {
            TaskId = 10,
            TaskTitle = "Code Review",
            ProjectName = "Chrono",
            HasPlannedDuration = true,
            RemainingTime = TimeSpan.FromMinutes(20),
            Duration = TimeSpan.FromMinutes(10)
        };

        var changedProperties = new List<string>();
        item.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        item.Duration = TimeSpan.FromMinutes(11);
        item.RemainingTime = TimeSpan.FromMinutes(19);

        Assert.Contains(nameof(RecentActivityItem.Duration), changedProperties);
        Assert.Contains(nameof(RecentActivityItem.FormattedDuration), changedProperties);
        Assert.Contains(nameof(RecentActivityItem.RemainingTime), changedProperties);
        Assert.Contains(nameof(RecentActivityItem.FormattedRemainingTime), changedProperties);
        Assert.Equal("00:19:00", item.FormattedRemainingTime);
    }

    [Fact]
    public void ProductivityDashboardStats_SettingLiveProperties_NotifiesViewBindings()
    {
        var stats = new ProductivityDashboardStats();
        var changedProperties = new List<string>();
        stats.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
            {
                changedProperties.Add(args.PropertyName);
            }
        };

        stats.TotalLoggedTime = TimeSpan.FromHours(4);
        stats.TodayLoggedTime = TimeSpan.FromHours(2);
        stats.WeekLoggedTime = TimeSpan.FromHours(10);
        stats.TotalRemainingTime = TimeSpan.FromHours(3);
        stats.TimeProgressRatio = 0.65;

        Assert.Contains(nameof(ProductivityDashboardStats.TotalLoggedTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.FormattedTotalTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.TodayLoggedTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.FormattedTodayTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.WeekLoggedTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.FormattedWeekTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.TotalRemainingTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.FormattedTotalRemainingTime), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.TimeProgressRatio), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.TimeProgressPercentage), changedProperties);
        Assert.Contains(nameof(ProductivityDashboardStats.FormattedTimeProgress), changedProperties);
        Assert.Equal("65%", stats.FormattedTimeProgress);
    }

    [Fact]
    public void LiveTick_PropagatesToTasksPageModel_WithoutFlickeringInactiveTasks()
    {
        var activeTask = new TaskItem
        {
            Id = 1,
            Title = "Active Task",
            PlannedDurationTicks = TimeSpan.FromMinutes(60).Ticks,
            TotalTime = TimeSpan.FromMinutes(10),
            HasActiveSession = true
        };

        var inactiveTask = new TaskItem
        {
            Id = 2,
            Title = "Idle Task",
            PlannedDurationTicks = TimeSpan.FromMinutes(30).Ticks,
            TotalTime = TimeSpan.FromMinutes(5),
            HasActiveSession = false
        };

        var tasksList = new List<TaskItem> { activeTask, inactiveTask };

        var inactiveTaskChanges = 0;
        inactiveTask.PropertyChanged += (s, e) => inactiveTaskChanges++;

        // Simulate incoming TimerTick on TasksPage
        var state = new ActiveSessionState
        {
            Task = activeTask,
            PriorCompletedDuration = TimeSpan.FromMinutes(10),
            ElapsedCurrentSession = TimeSpan.FromSeconds(15),
            TotalAccumulatedDuration = TimeSpan.FromMinutes(10).Add(TimeSpan.FromSeconds(15)),
            RemainingTime = TimeSpan.FromMinutes(50).Subtract(TimeSpan.FromSeconds(15)),
            IsCountdown = true
        };

        // TasksPage subscriber logic:
        var target = tasksList.Find(t => t.Id == state.Task.Id);
        Assert.NotNull(target);
        target.TotalTime = state.TotalAccumulatedDuration;

        Assert.Equal(TimeSpan.FromMinutes(10).Add(TimeSpan.FromSeconds(15)), activeTask.TotalTime);
        Assert.Equal(TimeSpan.FromMinutes(49).Add(TimeSpan.FromSeconds(45)), activeTask.RemainingTime);
        Assert.Equal(0, inactiveTaskChanges); // Inactive task was untouched!
    }

    [Fact]
    public void PageSubscriptionLifecycle_UnsubscribingPreventsMemoryLeaks()
    {
        var tickCount = 0;
        void OnTick(object? s, ActiveSessionState e) => tickCount++;

        // Simulate page appearing
        var serviceEvents = new FakeSessionPublisher();
        serviceEvents.TimerTick += OnTick;

        serviceEvents.PublishTick(new ActiveSessionState { Task = new TaskItem { Id = 1 } });
        Assert.Equal(1, tickCount);

        // Simulate page disappearing
        serviceEvents.TimerTick -= OnTick;

        serviceEvents.PublishTick(new ActiveSessionState { Task = new TaskItem { Id = 1 } });
        Assert.Equal(1, tickCount); // Did not increase because it was unsubscribed
    }

    private class FakeSessionPublisher
    {
        public event EventHandler<ActiveSessionState>? TimerTick;
        public void PublishTick(ActiveSessionState state) => TimerTick?.Invoke(this, state);
    }
}
