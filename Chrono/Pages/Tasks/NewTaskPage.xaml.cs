using Chrono.Services;
using Chrono.Models;
using TaskStatus = Chrono.Models.TaskStatus;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(ProjectId), "projectId")]
[QueryProperty(nameof(TaskId), "taskId")]
public partial class NewTaskPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private TaskItem? _existingTask;

    public string ProjectId { get; set; } = string.Empty;
    public string? TaskId { get; set; }

    public NewTaskPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrEmpty(TaskId) && int.TryParse(TaskId, out int taskId) && taskId > 0)
        {
            _existingTask = await _databaseService.GetTaskByIdAsync(taskId);
            if (_existingTask != null)
            {
                Title = "Editar Tarea";
                PageHeaderLabel.Text = "Editar Tarea";
                TitleEntry.Text = _existingTask.Title;
                DescriptionEditor.Text = _existingTask.Description;
                SaveButton.Text = "Actualizar";
                return;
            }
        }

        Title = "Nueva Tarea";
        PageHeaderLabel.Text = "Nueva Tarea";
        SaveButton.Text = "Guardar";
    }

    public async void OnSaveClicked(object? sender, EventArgs e)
    {
        var title = TitleEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlertAsync("Validación", "El título de la tarea es obligatorio.", "Aceptar");
            return;
        }

        if (_existingTask != null)
        {
            _existingTask.Title = title;
            _existingTask.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;
            await _databaseService.UpdateTaskAsync(_existingTask);
        }
        else
        {
            if (!int.TryParse(ProjectId, out int projectId) || projectId <= 0)
            {
                await DisplayAlertAsync("Error", "No se ha especificado un proyecto válido para la tarea.", "Aceptar");
                return;
            }

            var task = new TaskItem
            {
                ProjectId = projectId,
                Title = title,
                Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
                Status = TaskStatus.NotStarted,
                CreatedAt = DateTime.Now
            };
            await _databaseService.SaveTaskAsync(task);
        }

        await Shell.Current.GoToAsync("..");
    }
}