using Chrono.Models;
using Chrono.Services;
using System.Collections.ObjectModel;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(ProjectId), "projectId")]
public partial class TasksPage : ContentPage
{
    private readonly DatabaseService _databaseService;
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

    public TasksPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProjectAndTasksAsync();
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
            var activeSession = await _databaseService.GetActiveTimeEntryAsync();

            Tasks.Clear();
            foreach (var task in tasks)
            {
                task.TotalTime = await _databaseService.GetTotalDurationByTaskAsync(task.Id);
                task.HasActiveSession = (activeSession != null && activeSession.TaskId == task.Id);
                Tasks.Add(task);
            }
        }
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