using Chrono.Models;
using Chrono.Services;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(TaskId), "taskId")]
[QueryProperty(nameof(ProjectId), "projectId")]
public partial class TimerPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ITaskTimerService _timerService;

    private TaskItem? _task;
    private Project? _project;
    private bool _isProcessing;

    public string TaskId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;

    public TimerPage(DatabaseService databaseService, ITaskTimerService timerService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _timerService = timerService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _timerService.TimerTick += OnTimerTick;
        _timerService.SessionCompleted += OnSessionCompleted;
        _timerService.SessionStarted += OnSessionStarted;

        try
        {
            await _timerService.ReconcileActiveSessionAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reconciling on appearing: {ex.Message}");
        }

        await LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _timerService.TimerTick -= OnTimerTick;
        _timerService.SessionCompleted -= OnSessionCompleted;
        _timerService.SessionStarted -= OnSessionStarted;
    }

    private async Task LoadDataAsync()
    {
        if (!int.TryParse(TaskId, out int taskId) || taskId <= 0)
            return;

        _task = await _databaseService.GetTaskByIdAsync(taskId);
        if (_task == null)
            return;

        TaskTitleLabel.Text = _task.Title;
        TaskStatusLabel.Text = $"Estado: {_task.StatusDisplay}";

        int projectIdToFind = _task.ProjectId;
        if (int.TryParse(ProjectId, out int pId) && pId > 0)
        {
            projectIdToFind = pId;
        }

        _project = await _databaseService.GetProjectByIdAsync(projectIdToFind);
        if (_project != null)
        {
            ProjectNameLabel.Text = $"Proyecto: {_project.Name}";
        }

        // Total accumulated duration from completed sessions
        var priorDuration = await _databaseService.GetTotalDurationByTaskAsync(_task.Id);
        _task.TotalTime = priorDuration;

        PlannedBudgetLabel.Text = _task.HasPlannedDuration ? _task.FormattedPlannedDuration : "Sin límite";
        TotalAccumulatedTimeLabel.Text = _task.FormattedTotalTime;

        // Check active session state via timer service
        var activeState = await _timerService.GetActiveSessionStateAsync();

        if (activeState != null)
        {
            if (activeState.Task.Id == _task.Id)
            {
                // Active session belongs to THIS task
                SetRunningUi(activeState);
            }
            else
            {
                // Active session belongs to another task
                SetBlockedUi(activeState);
            }
        }
        else
        {
            // No active session
            SetStoppedUi();
        }
    }

    private void SetRunningUi(ActiveSessionState state)
    {
        StatusBadgeLabel.Text = state.IsCountdown ? "● CUENTA REGRESIVA EN CURSO" : "● CRONÓMETRO EN EJECUCIÓN";
        StatusBadgeLabel.TextColor = Color.FromArgb("#28A745");

        TimerModeLabel.Text = state.IsCountdown ? "TIEMPO RESTANTE" : "TIEMPO TRANSCURRIDO";
        TimerDisplayLabel.Text = state.IsCountdown ? state.FormattedRemainingTime : state.FormattedCurrentElapsed;

        CurrentSessionElapsedLabel.Text = state.FormattedCurrentElapsed;
        TotalAccumulatedTimeLabel.Text = state.FormattedTotalAccumulated;

        SessionStartLabel.Text = $"Iniciado: {state.Entry.Start:HH:mm:ss} ({state.Entry.Start:dd/MM/yyyy})";

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = true;
    }

    private void SetStoppedUi()
    {
        if (_task == null) return;

        bool isCountdown = _task.HasPlannedDuration;
        TimerModeLabel.Text = isCountdown ? "TIEMPO RESTANTE" : "CRONÓMETRO LIBRE";

        if (isCountdown)
        {
            TimerDisplayLabel.Text = _task.FormattedRemainingTime;
            if (_task.RemainingTime <= TimeSpan.Zero)
            {
                StatusBadgeLabel.Text = "PRESUPUESTO COMPLETADO";
                StatusBadgeLabel.TextColor = Color.FromArgb("#512BD4");
                SessionStartLabel.Text = "El tiempo planificado ha sido completado";
                StartButton.IsEnabled = false;
            }
            else
            {
                StatusBadgeLabel.Text = "DETENIDO";
                StatusBadgeLabel.TextColor = Colors.Gray;
                SessionStartLabel.Text = "Sin sesión activa";
                StartButton.IsEnabled = true;
            }
        }
        else
        {
            TimerDisplayLabel.Text = "00:00:00";
            StatusBadgeLabel.Text = "DETENIDO";
            StatusBadgeLabel.TextColor = Colors.Gray;
            SessionStartLabel.Text = "Sin sesión activa";
            StartButton.IsEnabled = true;
        }

        CurrentSessionElapsedLabel.Text = "00:00:00";
        TotalAccumulatedTimeLabel.Text = _task.FormattedTotalTime;
        StopButton.IsEnabled = false;
    }

    private void SetBlockedUi(ActiveSessionState otherState)
    {
        StatusBadgeLabel.Text = "⚠ SESIÓN ACTIVA EN OTRA TAREA";
        StatusBadgeLabel.TextColor = Colors.OrangeRed;
        SessionStartLabel.Text = $"Hay una sesión activa en '{otherState.Task.Title}'. Debes detenerla primero.";

        StartButton.IsEnabled = false;
        StopButton.IsEnabled = false;

        if (_task != null && _task.HasPlannedDuration)
        {
            TimerModeLabel.Text = "TIEMPO RESTANTE";
            TimerDisplayLabel.Text = _task.FormattedRemainingTime;
        }
        else
        {
            TimerModeLabel.Text = "CRONÓMETRO LIBRE";
            TimerDisplayLabel.Text = "00:00:00";
        }
    }

    private void OnTimerTick(object? sender, ActiveSessionState state)
    {
        if (_task == null || state.Task.Id != _task.Id)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (state.IsCountdown)
            {
                TimerDisplayLabel.Text = state.FormattedRemainingTime;
            }
            else
            {
                TimerDisplayLabel.Text = state.FormattedCurrentElapsed;
            }

            CurrentSessionElapsedLabel.Text = state.FormattedCurrentElapsed;
            TotalAccumulatedTimeLabel.Text = state.FormattedTotalAccumulated;
        });
    }

    private void OnSessionStarted(object? sender, ActiveSessionState state)
    {
        if (_task == null || state.Task.Id != _task.Id)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            SetRunningUi(state);
            TaskStatusLabel.Text = $"Estado: {state.Task.StatusDisplay}";
        });
    }

    private void OnSessionCompleted(object? sender, ActiveSessionState state)
    {
        if (_task == null || state.Task.Id != _task.Id)
            return;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadDataAsync();
        });
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (_task == null || _isProcessing) return;
        _isProcessing = true;

        try
        {
            var state = await _timerService.StartSessionAsync(_task.Id);
            SetRunningUi(state);
            TaskStatusLabel.Text = $"Estado: {_task.StatusDisplay}";
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Sesión", ex.Message, "Aceptar");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo iniciar la sesión: {ex.Message}", "Aceptar");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async void OnStopClicked(object? sender, EventArgs e)
    {
        if (_task == null || _isProcessing) return;
        _isProcessing = true;

        try
        {
            var state = await _timerService.StopSessionAsync(_task.Id);
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"No se pudo detener la sesión: {ex.Message}", "Aceptar");
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        if (_task == null) return;
        await Shell.Current.GoToAsync($"{nameof(SessionHistoryPage)}?taskId={_task.Id}");
    }
}
