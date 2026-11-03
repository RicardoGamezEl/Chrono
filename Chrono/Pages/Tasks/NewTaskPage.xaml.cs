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

                var pd = _existingTask.PlannedDuration;
                HoursEntry.Text = ((int)pd.TotalHours).ToString();
                MinutesEntry.Text = pd.Minutes.ToString();
                SecondsEntry.Text = pd.Seconds.ToString();

                SaveButton.Text = "Actualizar";
                return;
            }
        }

        Title = "Nueva Tarea";
        PageHeaderLabel.Text = "Nueva Tarea";
        HoursEntry.Text = "0";
        MinutesEntry.Text = "0";
        SecondsEntry.Text = "0";
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

        // Validate planned duration inputs
        var hoursStr = string.IsNullOrWhiteSpace(HoursEntry.Text) ? "0" : HoursEntry.Text.Trim();
        var minutesStr = string.IsNullOrWhiteSpace(MinutesEntry.Text) ? "0" : MinutesEntry.Text.Trim();
        var secondsStr = string.IsNullOrWhiteSpace(SecondsEntry.Text) ? "0" : SecondsEntry.Text.Trim();

        if (!int.TryParse(hoursStr, out int hours) || hours < 0 || hours > 100000)
        {
            await DisplayAlertAsync("Validación", "Las horas deben ser un número entero mayor o igual a 0.", "Aceptar");
            return;
        }

        if (!int.TryParse(minutesStr, out int minutes) || minutes < 0 || minutes > 59)
        {
            await DisplayAlertAsync("Validación", "Los minutos deben ser un valor entre 0 y 59.", "Aceptar");
            return;
        }

        if (!int.TryParse(secondsStr, out int seconds) || seconds < 0 || seconds > 59)
        {
            await DisplayAlertAsync("Validación", "Los segundos deben ser un valor entre 0 y 59.", "Aceptar");
            return;
        }

        TimeSpan plannedDuration;
        try
        {
            plannedDuration = new TimeSpan(hours, minutes, seconds);
        }
        catch (OverflowException)
        {
            await DisplayAlertAsync("Validación", "La duración ingresada supera el límite máximo permitido.", "Aceptar");
            return;
        }

        if (_existingTask != null)
        {
            _existingTask.Title = title;
            _existingTask.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;
            _existingTask.PlannedDurationTicks = plannedDuration.Ticks;
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
                PlannedDurationTicks = plannedDuration.Ticks,
                Status = TaskStatus.NotStarted,
                CreatedAt = DateTime.Now
            };
            await _databaseService.SaveTaskAsync(task);
        }

        await Shell.Current.GoToAsync("..");
    }
}