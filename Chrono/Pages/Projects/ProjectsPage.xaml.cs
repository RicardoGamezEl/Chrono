using Chrono.Models;
using Chrono.Pages.Tasks;
using Chrono.Services;
using System.Collections.ObjectModel;
namespace Chrono;

public partial class ProjectsPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    public ProjectsPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        Projects = new ObservableCollection<Project>();
        BindingContext = this;
        InitializeComponent();
    }
    public ObservableCollection<Project> Projects { get; private set; }
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var projects = await _databaseService.GetProjectsAsync();
        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(project);
        }
    }
    private async void OnNewProjectClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewProjectPage));
    }
    private async void OnProjectClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        var project =(Project)button.CommandParameter;

        await Shell.Current.GoToAsync($"{nameof(TasksPage)}?projectId={project.Id}");
    }

}