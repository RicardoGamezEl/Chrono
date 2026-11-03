using System;
using System.Threading.Tasks;
using Chrono.Models;
using Chrono.Pages.Tasks;
using Chrono.Services;

namespace Chrono;

public partial class MainPage : ContentPage
{
    private readonly StatsService _statsService;
    private readonly ITaskTimerService _timerService;

    private ProductivityDashboardStats? _stats;
    private TimeSpan _baseTotalTime = TimeSpan.Zero;
    private TimeSpan _baseTodayTime = TimeSpan.Zero;
    private TimeSpan _baseWeekTime = TimeSpan.Zero;
    private TimeSpan _baseRemainingTime = TimeSpan.Zero;
    private TimeSpan _baseWorkedOnPlanned = TimeSpan.Zero;
    private TimeSpan _basePlannedTime = TimeSpan.Zero;

    public MainPage(StatsService statsService, ITaskTimerService timerService)
    {
        InitializeComponent();
        _statsService = statsService;
        _timerService = timerService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _timerService.TimerTick += OnTimerTick;
        _timerService.SessionStarted += OnSessionStarted;
        _timerService.SessionCompleted += OnSessionCompleted;

        try
        {
            await _timerService.ReconcileActiveSessionAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reconciling on dashboard: {ex.Message}");
        }

        await LoadDashboardAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _timerService.TimerTick -= OnTimerTick;
        _timerService.SessionStarted -= OnSessionStarted;
        _timerService.SessionCompleted -= OnSessionCompleted;
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            _stats = await _statsService.GetDashboardStatsAsync();
            BindingContext = _stats;

            // Cache base durations without ongoing session elapsed time
            var activeState = await _timerService.GetActiveSessionStateAsync();
            var activeElapsed = activeState?.ElapsedCurrentSession ?? TimeSpan.Zero;

            _baseTotalTime = _stats.TotalLoggedTime >= activeElapsed
                ? _stats.TotalLoggedTime - activeElapsed
                : TimeSpan.Zero;

            _baseTodayTime = _stats.TodayLoggedTime >= activeElapsed
                ? _stats.TodayLoggedTime - activeElapsed
                : TimeSpan.Zero;

            _baseWeekTime = _stats.WeekLoggedTime >= activeElapsed
                ? _stats.WeekLoggedTime - activeElapsed
                : TimeSpan.Zero;

            _basePlannedTime = _stats.TotalPlannedTime;
            _baseRemainingTime = _stats.TotalRemainingTime;
            _baseWorkedOnPlanned = _stats.TotalWorkedOnPlannedTime;

            EmptyProjectsView.IsVisible = !_stats.HasProjects;
            ProjectsStack.IsVisible = _stats.HasProjects;

            EmptyActivitiesView.IsVisible = !_stats.HasActivities;
            RecentActivitiesStack.IsVisible = _stats.HasActivities;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
        }
    }

    private void OnTimerTick(object? sender, ActiveSessionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_stats == null) return;

            // 1. Update or create ActiveSession item
            if (_stats.ActiveSession == null || _stats.ActiveSession.TaskId != state.Task.Id)
            {
                _stats.ActiveSession = new RecentActivityItem
                {
                    TimeEntryId = state.Entry.Id,
                    TaskId = state.Task.Id,
                    TaskTitle = state.Task.Title,
                    ProjectId = state.Project?.Id ?? state.Task.ProjectId,
                    ProjectName = state.Project?.Name ?? "Proyecto",
                    Start = state.Entry.Start,
                    End = null,
                    Duration = state.ElapsedCurrentSession,
                    HasPlannedDuration = state.IsCountdown,
                    RemainingTime = state.RemainingTime
                };
            }
            else
            {
                _stats.ActiveSession.Duration = state.ElapsedCurrentSession;
                _stats.ActiveSession.RemainingTime = state.RemainingTime;
                _stats.ActiveSession.HasPlannedDuration = state.IsCountdown;
            }

            // 2. Update dashboard live accumulated metrics
            _stats.TotalLoggedTime = _baseTotalTime + state.ElapsedCurrentSession;
            _stats.TodayLoggedTime = _baseTodayTime + state.ElapsedCurrentSession;
            _stats.WeekLoggedTime = _baseWeekTime + state.ElapsedCurrentSession;

            if (state.IsCountdown)
            {
                var maxForSession = state.Task.PlannedDuration > state.PriorCompletedDuration
                    ? state.Task.PlannedDuration - state.PriorCompletedDuration
                    : TimeSpan.Zero;

                var sessionWorkedCapped = state.ElapsedCurrentSession <= maxForSession
                    ? state.ElapsedCurrentSession
                    : maxForSession;

                var liveRemaining = _baseRemainingTime - sessionWorkedCapped;
                _stats.TotalRemainingTime = liveRemaining > TimeSpan.Zero ? liveRemaining : TimeSpan.Zero;

                if (_basePlannedTime > TimeSpan.Zero)
                {
                    var liveWorked = _baseWorkedOnPlanned + sessionWorkedCapped;
                    _stats.TimeProgressRatio = Math.Min(1.0, Math.Max(0.0, liveWorked.TotalSeconds / _basePlannedTime.TotalSeconds));
                }
            }
        });
    }

    private void OnSessionStarted(object? sender, ActiveSessionState state)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadDashboardAsync();
        });
    }

    private void OnSessionCompleted(object? sender, ActiveSessionState state)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_stats != null)
            {
                _stats.ActiveSession = null;
            }
            await LoadDashboardAsync();
        });
    }

    private async void OnActiveSessionClicked(object? sender, EventArgs e)
    {
        if (BindingContext is ProductivityDashboardStats stats && stats.ActiveSession != null)
        {
            await Shell.Current.GoToAsync($"{nameof(TimerPage)}?taskId={stats.ActiveSession.TaskId}&projectId={stats.ActiveSession.ProjectId}");
        }
    }

    private async void OnProjectsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProjectsPage));
    }

    private async void OnNewProjectClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewProjectPage));
    }

    private async void OnOpenProjectClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ProjectProgressStats project)
        {
            await Shell.Current.GoToAsync($"{nameof(TasksPage)}?projectId={project.ProjectId}");
        }
    }
}