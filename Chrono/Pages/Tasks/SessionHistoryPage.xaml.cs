using Chrono.Models;
using Chrono.Services;
using System.Collections.ObjectModel;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(TaskId), "taskId")]
public partial class SessionHistoryPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private TaskItem? _task;

    public string TaskId { get; set; } = string.Empty;
    public ObservableCollection<TimeEntry> Entries { get; } = new();

    public SessionHistoryPage(DatabaseService databaseService)
    {
        InitializeComponent();
        _databaseService = databaseService;
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        if (!int.TryParse(TaskId, out int taskId) || taskId <= 0)
            return;

        _task = await _databaseService.GetTaskByIdAsync(taskId);
        if (_task != null)
        {
            TaskTitleLabel.Text = $"Historial: {_task.Title}";
            Title = $"Historial - {_task.Title}";
        }

        var entries = await _databaseService.GetTimeEntriesByTaskAsync(taskId);
        Entries.Clear();
        var total = TimeSpan.Zero;

        foreach (var entry in entries)
        {
            Entries.Add(entry);
            total += entry.Duration;
        }

        TotalTimeLabel.Text = $"Tiempo total acumulado: {(int)total.TotalHours:D2}:{total.Minutes:D2}:{total.Seconds:D2}";
    }

    private async void OnDeleteEntryClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is TimeEntry entry)
        {
            bool confirm = await DisplayAlertAsync(
                "Eliminar Sesión",
                $"¿Deseas eliminar el registro de sesión con duración {entry.FormattedDuration}?",
                "Eliminar",
                "Cancelar");

            if (confirm)
            {
                await _databaseService.DeleteTimeEntryAsync(entry.Id);
                Entries.Remove(entry);

                var total = TimeSpan.Zero;
                foreach (var item in Entries)
                {
                    total += item.Duration;
                }
                TotalTimeLabel.Text = $"Tiempo total acumulado: {(int)total.TotalHours:D2}:{total.Minutes:D2}:{total.Seconds:D2}";
            }
        }
    }
}
