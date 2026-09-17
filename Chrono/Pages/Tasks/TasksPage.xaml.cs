using Chrono.Models;
using Chrono.Services;
using System.Collections.ObjectModel;

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
            ProjectIdLabel.Text = $"Project ID: {value}";
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
        await LoadTaskAsync();
    }
    public async Task LoadTaskAsync()
    {
        int projectId = int.Parse(ProjectId);
        var tasks = await _databaseService.GetTaskByProjectAsync(projectId);
        Tasks.Clear();
        foreach (var task in tasks)
        {
            Tasks.Add(task);
        }
    }
    public async void OnNewTaskClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"{nameof(NewTaskPage)}?projectId={ProjectId}");
    }
}