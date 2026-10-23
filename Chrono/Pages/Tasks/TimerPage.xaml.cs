using Chrono.Models;
using Chrono.Services;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(TaskId), "taskId")]
[QueryProperty(nameof(ProjectId), "projectId")]
public partial class TimerPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private TaskItem? _task;
    private Project? _project;
    private TimeEntry? _activeEntry;
    private IDispatcherTimer? _dispatcherTimer;

    public string TaskId { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;

    public TimerPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_dispatcherTimer == null)
        {
            _dispatcherTimer = Dispatcher.CreateTimer();
            _dispatcherTimer.Interval = TimeSpan.FromSeconds(1);
            _dispatcherTimer.Tick += OnTimerTick;
        }

        await LoadDataAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _dispatcherTimer?.Stop();
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

        // Check for active sessions across entire app
        var globalActive = await _databaseService.GetActiveTimeEntryAsync();

        if (globalActive != null)
        {
            if (globalActive.TaskId == _task.Id)
            {
                // Active session belongs to THIS task -> resume counting seamlessly
                _activeEntry = globalActive;
                SetRunningUi(true);
                UpdateTimerDisplay();
                _dispatcherTimer?.Start();
            }
            else
            {
                // Active session belongs to another task
                _activeEntry = null;
                _dispatcherTimer?.Stop();
                SetBlockedUi();
            }
        }
        else
        {
            // No active session anywhere
            _activeEntry = null;
            _dispatcherTimer?.Stop();
            SetRunningUi(false);
            TimerDisplayLabel.Text = "00:00:00";
            SessionStartLabel.Text = "Sin sesión activa";
        }

        await RefreshTotalTimeAsync();
    }

    private async Task RefreshTotalTimeAsync()
    {
        if (_task == null) return;
        var total = await _databaseService.GetTotalDurationByTaskAsync(_task.Id);
        TotalAccumulatedTimeLabel.Text = $"{(int)total.TotalHours:D2}:{total.Minutes:D2}:{total.Seconds:D2}";
    }

    private void SetRunningUi(bool isRunning)
    {
        if (isRunning)
        {
            StatusBadgeLabel.Text = "● EN EJECUCIÓN";
            StatusBadgeLabel.TextColor = Color.FromArgb("#28A745");
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            if (_activeEntry != null)
            {
                SessionStartLabel.Text = $"Iniciado: {_activeEntry.Start:HH:mm:ss} ({_activeEntry.Start:dd/MM/yyyy})";
            }
        }
        else
        {
            StatusBadgeLabel.Text = "DETENIDO";
            StatusBadgeLabel.TextColor = Colors.Gray;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }
    }

    private void SetBlockedUi()
    {
        StatusBadgeLabel.Text = "⚠ SESIÓN ACTIVA EN OTRA TAREA";
        StatusBadgeLabel.TextColor = Colors.OrangeRed;
        SessionStartLabel.Text = "Hay una sesión de tiempo activa en otra tarea. Debes detenerla primero.";
        StartButton.IsEnabled = false;
        StopButton.IsEnabled = false;
        TimerDisplayLabel.Text = "00:00:00";
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        UpdateTimerDisplay();
    }

    private void UpdateTimerDisplay()
    {
        if (_activeEntry != null)
        {
            var elapsed = DateTime.Now >= _activeEntry.Start
                ? DateTime.Now - _activeEntry.Start
                : TimeSpan.Zero;
            TimerDisplayLabel.Text = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }
    }

    private async void OnStartClicked(object? sender, EventArgs e)
    {
        if (_task == null) return;

        // Verify no simultaneous active session anywhere in the app
        var globalActive = await _databaseService.GetActiveTimeEntryAsync();
        if (globalActive != null)
        {
            if (globalActive.TaskId != _task.Id)
            {
                await DisplayAlertAsync("Sesión Activa", "Ya existe una sesión de tiempo activa en otra tarea. Detén esa sesión antes de iniciar una nueva.", "Aceptar");
                return;
            }
            // Already active on this task
            _activeEntry = globalActive;
            SetRunningUi(true);
            _dispatcherTimer?.Start();
            return;
        }

        var newEntry = new TimeEntry
        {
            TaskId = _task.Id,
            Start = DateTime.Now,
            End = null
        };

        await _databaseService.SaveTimeEntryAsync(newEntry);
        _activeEntry = newEntry;

        if (_task.Status == TaskStatus.NotStarted)
        {
            _task.Status = TaskStatus.InProgress;
            await _databaseService.UpdateTaskAsync(_task);
            TaskStatusLabel.Text = $"Estado: {_task.StatusDisplay}";
        }

        SetRunningUi(true);
        UpdateTimerDisplay();
        _dispatcherTimer?.Start();
    }

    private async void OnStopClicked(object? sender, EventArgs e)
    {
        if (_activeEntry == null) return;

        _dispatcherTimer?.Stop();
        _activeEntry.End = DateTime.Now;
        await _databaseService.UpdateTimeEntryAsync(_activeEntry);

        var duration = _activeEntry.Duration;
        _activeEntry = null;

        SetRunningUi(false);
        TimerDisplayLabel.Text = "00:00:00";
        SessionStartLabel.Text = "Sesión finalizada con éxito";

        await RefreshTotalTimeAsync();

        await DisplayAlertAsync(
            "Sesión Finalizada",
            $"Sesión registrada correctamente.\nDuración: {(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}",
            "Aceptar");
    }

    private async void OnHistoryClicked(object? sender, EventArgs e)
    {
        if (_task == null) return;
        await Shell.Current.GoToAsync($"{nameof(SessionHistoryPage)}?taskId={_task.Id}");
    }
}
