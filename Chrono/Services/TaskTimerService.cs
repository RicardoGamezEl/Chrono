using System;
using System.Threading;
using System.Threading.Tasks;
using Chrono.Models;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Services;

public class TaskTimerService : ITaskTimerService, IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly INotificationService? _notificationService;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly System.Timers.Timer _ticker;

    private ActiveSessionState? _currentSession;
    private bool _isDisposed;
    private bool _isReconciling;

    public ActiveSessionState? CurrentSession => _currentSession;
    public bool HasActiveSession => _currentSession != null;
    public bool IsTickerEnabled { get; set; } = true;

    public event EventHandler<ActiveSessionState>? TimerTick;
    public event EventHandler<ActiveSessionState>? SessionCompleted;
    public event EventHandler<ActiveSessionState>? SessionStarted;

    public TaskTimerService(
        DatabaseService databaseService,
        INotificationService? notificationService = null,
        bool enableTicker = true)
    {
        _databaseService = databaseService;
        _notificationService = notificationService;
        IsTickerEnabled = enableTicker;

        _ticker = new System.Timers.Timer(1000);
        _ticker.AutoReset = true;
        _ticker.Elapsed += OnTickerElapsed;
    }

    private async void OnTickerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_isReconciling) return;
        _isReconciling = true;

        try
        {
            ActiveSessionState? stateToNotify = null;
            bool shouldStopExpired = false;
            DateTime? expirationTime = null;
            int? expiredTaskId = null;

            await _lock.WaitAsync();
            try
            {
                if (_currentSession == null)
                {
                    _currentSession = await LoadActiveSessionFromDbAsync(DateTime.Now);
                }

                if (_currentSession != null)
                {
                    var now = DateTime.Now;
                    var elapsed = now >= _currentSession.Entry.Start ? now - _currentSession.Entry.Start : TimeSpan.Zero;
                    _currentSession.ElapsedCurrentSession = elapsed;
                    _currentSession.TotalAccumulatedDuration = _currentSession.PriorCompletedDuration + elapsed;

                    if (_currentSession.IsCountdown)
                    {
                        var maxAllowed = _currentSession.Task.PlannedDuration > _currentSession.PriorCompletedDuration
                            ? _currentSession.Task.PlannedDuration - _currentSession.PriorCompletedDuration
                            : TimeSpan.Zero;

                        if (elapsed >= maxAllowed)
                        {
                            _currentSession.HasReachedBudget = true;
                            _currentSession.RemainingTime = TimeSpan.Zero;
                            shouldStopExpired = true;
                            expiredTaskId = _currentSession.Task.Id;
                            expirationTime = _currentSession.Entry.Start + maxAllowed;
                        }
                        else
                        {
                            _currentSession.RemainingTime = _currentSession.Task.PlannedDuration - _currentSession.TotalAccumulatedDuration;
                            stateToNotify = _currentSession;
                        }
                    }
                    else
                    {
                        _currentSession.RemainingTime = TimeSpan.Zero;
                        stateToNotify = _currentSession;
                    }
                }
                else
                {
                    _ticker.Stop();
                }
            }
            finally
            {
                _lock.Release();
            }

            if (shouldStopExpired && expiredTaskId.HasValue)
            {
                await StopSessionAsync(expiredTaskId.Value, expirationTime);
            }
            else if (stateToNotify != null)
            {
                TimerTick?.Invoke(this, stateToNotify);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error on ticker tick: {ex.Message}");
        }
        finally
        {
            _isReconciling = false;
        }
    }

    public async Task<ActiveSessionState?> GetActiveSessionStateAsync(DateTime? referenceNow = null)
    {
        await _lock.WaitAsync();
        try
        {
            if (_currentSession != null && referenceNow == null)
            {
                var now = DateTime.Now;
                var elapsed = now >= _currentSession.Entry.Start ? now - _currentSession.Entry.Start : TimeSpan.Zero;
                _currentSession.ElapsedCurrentSession = elapsed;
                _currentSession.TotalAccumulatedDuration = _currentSession.PriorCompletedDuration + elapsed;

                if (_currentSession.IsCountdown)
                {
                    var max = _currentSession.Task.PlannedDuration > _currentSession.PriorCompletedDuration
                        ? _currentSession.Task.PlannedDuration - _currentSession.PriorCompletedDuration
                        : TimeSpan.Zero;
                    _currentSession.RemainingTime = elapsed >= max
                        ? TimeSpan.Zero
                        : _currentSession.Task.PlannedDuration - _currentSession.TotalAccumulatedDuration;
                }
                return _currentSession;
            }

            _currentSession = await LoadActiveSessionFromDbAsync(referenceNow ?? DateTime.Now);
            return _currentSession;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<ActiveSessionState?> LoadActiveSessionFromDbAsync(DateTime now)
    {
        var active = await _databaseService.GetActiveTimeEntryAsync();
        if (active == null)
            return null;

        var task = await _databaseService.GetTaskByIdAsync(active.TaskId);
        if (task == null)
            return null;

        var project = await _databaseService.GetProjectByIdAsync(task.ProjectId);
        var prior = await _databaseService.GetTotalDurationByTaskAsync(task.Id, includeActive: false);

        var elapsed = now >= active.Start ? now - active.Start : TimeSpan.Zero;
        var isCountdown = task.HasPlannedDuration;
        TimeSpan remaining = TimeSpan.Zero;
        bool reachedBudget = false;

        if (isCountdown)
        {
            var total = prior + elapsed;
            if (total >= task.PlannedDuration)
            {
                remaining = TimeSpan.Zero;
                reachedBudget = true;
            }
            else
            {
                remaining = task.PlannedDuration - total;
            }
        }

        return new ActiveSessionState
        {
            Task = task,
            Project = project,
            Entry = active,
            ElapsedCurrentSession = elapsed,
            PriorCompletedDuration = prior,
            TotalAccumulatedDuration = prior + elapsed,
            RemainingTime = remaining,
            IsCountdown = isCountdown,
            HasReachedBudget = reachedBudget
        };
    }

    public async Task<TimeSpan> CalculateRemainingTimeAsync(int taskId, TimeSpan? currentSessionElapsed = null)
    {
        var task = await _databaseService.GetTaskByIdAsync(taskId);
        if (task == null || !task.HasPlannedDuration)
            return TimeSpan.Zero;

        var prior = await _databaseService.GetTotalDurationByTaskAsync(taskId, includeActive: false);
        var elapsed = currentSessionElapsed ?? TimeSpan.Zero;

        var total = prior + elapsed;
        if (total >= task.PlannedDuration)
            return TimeSpan.Zero;

        var remaining = task.PlannedDuration - total;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    public async Task<ActiveSessionState> StartSessionAsync(int taskId, DateTime? referenceStartTime = null)
    {
        await _lock.WaitAsync();
        try
        {
            var active = await _databaseService.GetActiveTimeEntryAsync();
            if (active != null)
            {
                if (active.TaskId == taskId)
                {
                    // Already active for this task, idempotent return
                    if (_currentSession == null || _currentSession.Task.Id != taskId)
                    {
                        _currentSession = await LoadActiveSessionFromDbAsync(referenceStartTime ?? DateTime.Now);
                    }
                    if (IsTickerEnabled) _ticker.Start();
                    return _currentSession!;
                }

                throw new InvalidOperationException("Ya existe una sesión activa en otra tarea. Detén esa sesión antes de iniciar una nueva.");
            }

            var task = await _databaseService.GetTaskByIdAsync(taskId)
                ?? throw new InvalidOperationException($"No se encontró la tarea con ID {taskId}.");

            var prior = await _databaseService.GetTotalDurationByTaskAsync(taskId, includeActive: false);
            if (task.HasPlannedDuration && prior >= task.PlannedDuration)
            {
                throw new InvalidOperationException("El presupuesto planificado para esta tarea ya ha sido completado.");
            }

            var now = referenceStartTime ?? DateTime.Now;
            var newEntry = new TimeEntry
            {
                TaskId = taskId,
                Start = now,
                End = null
            };

            await _databaseService.SaveTimeEntryAsync(newEntry);

            if (task.Status == TaskStatus.NotStarted)
            {
                task.Status = TaskStatus.InProgress;
                await _databaseService.UpdateTaskAsync(task);
            }

            var project = await _databaseService.GetProjectByIdAsync(task.ProjectId);
            var isCountdown = task.HasPlannedDuration;
            var remaining = isCountdown ? (task.PlannedDuration > prior ? task.PlannedDuration - prior : TimeSpan.Zero) : TimeSpan.Zero;

            _currentSession = new ActiveSessionState
            {
                Task = task,
                Project = project,
                Entry = newEntry,
                ElapsedCurrentSession = TimeSpan.Zero,
                PriorCompletedDuration = prior,
                TotalAccumulatedDuration = prior,
                RemainingTime = remaining,
                IsCountdown = isCountdown,
                HasReachedBudget = false
            };

            if (IsTickerEnabled) _ticker.Start();

            SessionStarted?.Invoke(this, _currentSession);
            return _currentSession;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ActiveSessionState?> StopSessionAsync(int? taskId = null, DateTime? referenceEndTime = null)
    {
        await _lock.WaitAsync();
        try
        {
            var active = await _databaseService.GetActiveTimeEntryAsync();
            if (active == null)
            {
                _currentSession = null;
                _ticker.Stop();
                return null;
            }

            if (taskId.HasValue && active.TaskId != taskId.Value)
                return null;

            var task = await _databaseService.GetTaskByIdAsync(active.TaskId);
            if (task == null)
                return null;

            var prior = await _databaseService.GetTotalDurationByTaskAsync(task.Id, includeActive: false);
            var now = referenceEndTime ?? DateTime.Now;
            if (now < active.Start)
                now = active.Start;

            var elapsed = now - active.Start;
            bool reachedBudget = false;

            if (task.HasPlannedDuration)
            {
                var maxAllowed = task.PlannedDuration > prior ? task.PlannedDuration - prior : TimeSpan.Zero;
                if (elapsed >= maxAllowed)
                {
                    reachedBudget = true;
                    active.End = active.Start + maxAllowed;
                }
                else
                {
                    active.End = now;
                }
            }
            else
            {
                active.End = now;
            }

            await _databaseService.UpdateTimeEntryAsync(active);
            _currentSession = null;
            _ticker.Stop();

            var project = await _databaseService.GetProjectByIdAsync(task.ProjectId);
            var duration = active.Duration;
            var totalAccumulated = prior + duration;
            var remaining = task.HasPlannedDuration
                ? (task.PlannedDuration > totalAccumulated ? task.PlannedDuration - totalAccumulated : TimeSpan.Zero)
                : TimeSpan.Zero;

            var state = new ActiveSessionState
            {
                Task = task,
                Project = project,
                Entry = active,
                ElapsedCurrentSession = duration,
                PriorCompletedDuration = prior,
                TotalAccumulatedDuration = totalAccumulated,
                RemainingTime = remaining,
                IsCountdown = task.HasPlannedDuration,
                HasReachedBudget = reachedBudget
            };

            if (reachedBudget)
            {
                _notificationService?.ShowToast(
                    "Tiempo cumplido",
                    $"La tarea '{task.Title}' completó su duración planificada de {task.FormattedPlannedDuration}.");
            }
            else
            {
                _notificationService?.ShowToast(
                    "Sesión Finalizada",
                    $"Sesión registrada para '{task.Title}'. Duración: {(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}");
            }

            SessionCompleted?.Invoke(this, state);
            return state;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<ActiveSessionState?> ReconcileActiveSessionAsync(DateTime? referenceNow = null)
    {
        await _lock.WaitAsync();
        try
        {
            var active = await _databaseService.GetActiveTimeEntryAsync();
            if (active == null)
            {
                _currentSession = null;
                _ticker.Stop();
                return null;
            }

            var task = await _databaseService.GetTaskByIdAsync(active.TaskId);
            if (task == null)
            {
                _currentSession = null;
                _ticker.Stop();
                return null;
            }

            var prior = await _databaseService.GetTotalDurationByTaskAsync(task.Id, includeActive: false);
            var now = referenceNow ?? DateTime.Now;
            var elapsed = now >= active.Start ? now - active.Start : TimeSpan.Zero;

            if (task.HasPlannedDuration)
            {
                var maxAllowed = task.PlannedDuration > prior ? task.PlannedDuration - prior : TimeSpan.Zero;
                if (elapsed >= maxAllowed)
                {
                    // Expired: finalize with exact budget interval
                    active.End = active.Start + maxAllowed;
                    await _databaseService.UpdateTimeEntryAsync(active);
                    _currentSession = null;
                    _ticker.Stop();

                    var project = await _databaseService.GetProjectByIdAsync(task.ProjectId);
                    var duration = active.Duration;
                    var total = prior + duration;

                    var expiredState = new ActiveSessionState
                    {
                        Task = task,
                        Project = project,
                        Entry = active,
                        ElapsedCurrentSession = duration,
                        PriorCompletedDuration = prior,
                        TotalAccumulatedDuration = total,
                        RemainingTime = TimeSpan.Zero,
                        IsCountdown = true,
                        HasReachedBudget = true
                    };

                    _notificationService?.ShowToast(
                        "Tiempo cumplido",
                        $"La tarea '{task.Title}' completó su duración planificada de {task.FormattedPlannedDuration}.");

                    SessionCompleted?.Invoke(this, expiredState);
                    return expiredState;
                }
            }

            // Session still ongoing: cache state and start ticker
            var currentProject = await _databaseService.GetProjectByIdAsync(task.ProjectId);
            var currentTotal = prior + elapsed;
            var currentRemaining = task.HasPlannedDuration
                ? (task.PlannedDuration > currentTotal ? task.PlannedDuration - currentTotal : TimeSpan.Zero)
                : TimeSpan.Zero;

            _currentSession = new ActiveSessionState
            {
                Task = task,
                Project = currentProject,
                Entry = active,
                ElapsedCurrentSession = elapsed,
                PriorCompletedDuration = prior,
                TotalAccumulatedDuration = currentTotal,
                RemainingTime = currentRemaining,
                IsCountdown = task.HasPlannedDuration,
                HasReachedBudget = false
            };

            if (IsTickerEnabled) _ticker.Start();

            return _currentSession;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _ticker.Stop();
        _ticker.Dispose();
        _lock.Dispose();
    }
}
