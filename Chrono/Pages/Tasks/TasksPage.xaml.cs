using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Chrono.Models;
using Chrono.Services;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(ProjectId), "projectId")]
public partial class TasksPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private readonly ITaskTimerService _timerService;
    private string _projectId = string.Empty;

    public ObservableCollection<TaskItem> Tasks { get; } = new();

    public string ProjectId
    {
        get => _projectId;
        set
        {
            _projectId = value;
            ProjectIdLabel.Text = $"ID del Proyecto: {value}";
        }
    }

    public TasksPage(DatabaseService databaseService, ITaskTimerService timerService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        _timerService = timerService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _timerService.TimerTick += OnTimerTick;
        _timerService.SessionStarted += OnSessionStarted;
        _timerService.SessionCompleted += OnSessionCompleted;

        await LoadProjectAndTasksAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _timerService.TimerTick -= OnTimerTick;
        _timerService.SessionStarted -= OnSessionStarted;
        _timerService.SessionCompleted -= OnSessionCompleted;
    }

    public async Task LoadProjectAndTasksAsync()
    {
        if (int.TryParse(ProjectId, out int projectId) && projectId > 0)
        {
            var project = await _databaseService.GetProjectByIdAsync(projectId);
            if (project != null)
            {
                TitleProject.Text = $"Tareas: {project.Name}";
                Title = project.Name;
            }

            var tasks = await _databaseService.GetTaskByProjectAsync(projectId);
            var activeSessionState = await _timerService.GetActiveSessionStateAsync();

            Tasks.Clear();
            foreach (var task in tasks)
            {
                var isThisActive = activeSessionState != null && activeSessionState.Task.Id == task.Id;
                if (isThisActive)
                {
                    task.TotalTime = activeSessionState!.TotalAccumulatedDuration;
                    task.HasActiveSession = true;
                }
                else
                {
                    task.TotalTime = await _databaseService.GetTotalDurationByTaskAsync(task.Id, includeActive: false);
                    task.HasActiveSession = false;
                }

                Tasks.Add(task);
            }
        }
    }

    private void OnTimerTick(object? sender, ActiveSessionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var task = Tasks.FirstOrDefault(t => t.Id == state.Task.Id);
            if (task != null)
            {
                if (!task.HasActiveSession)
                {
                    task.HasActiveSession = true;
                }
                task.TotalTime = state.TotalAccumulatedDuration;
            }
        });
    }

    private void OnSessionStarted(object? sender, ActiveSessionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var task in Tasks)
            {
                var isCurrent = task.Id == state.Task.Id;
                task.HasActiveSession = isCurrent;
                if (isCurrent)
                {
                    task.TotalTime = state.TotalAccumulatedDuration;
                }
            }
        });
    }

    private void OnSessionCompleted(object? sender, ActiveSessionState state)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var task = Tasks.FirstOrDefault(t => t.Id == state.Task.Id);
            if (task != null)
            {
                task.HasActiveSession = false;
                task.TotalTime = state.TotalAccumulatedDuration;
            }
        });
    }

    public async void OnNewTaskClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(NewTaskPage)}?projectId={ProjectId}");
    }

    public async void OnTimerClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TaskItem task)
        {
            await Shell.Current.GoToAsync($"{nameof(TimerPage)}?taskId={task.Id}&projectId={ProjectId}");
        }
    }

    public async void OnHistoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TaskItem task)
        {
            await Shell.Current.GoToAsync($"{nameof(SessionHistoryPage)}?taskId={task.Id}");
        }
    }

    public async void OnEditTaskClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TaskItem task)
        {
            await Shell.Current.GoToAsync($"{nameof(NewTaskPage)}?projectId={ProjectId}&taskId={task.Id}");
        }
    }

    public async void OnDeleteTaskClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TaskItem task)
        {
            bool confirm = await DisplayAlertAsync(
                "Eliminar Tarea",
                $"¿Estás seguro de que deseas eliminar la tarea '{task.Title}'? Se eliminarán también sus registros de tiempo asociados.",
                "Eliminar",
                "Cancelar");

            if (confirm)
            {
                await _databaseService.DeleteTaskAsync(task.Id);
                Tasks.Remove(task);
            }
        }
    }

    public async void OnStatusClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not TaskItem taskItem)
            return;

        taskItem.Status = taskItem.Status switch
        {
            TaskStatus.NotStarted => TaskStatus.InProgress,
            TaskStatus.InProgress => TaskStatus.Completed,
            _ => TaskStatus.NotStarted
        };

        await _databaseService.UpdateTaskAsync(taskItem);
    }
}