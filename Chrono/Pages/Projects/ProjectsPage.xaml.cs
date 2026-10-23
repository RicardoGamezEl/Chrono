using Chrono.Models;
using Chrono.Pages.Tasks;
using Chrono.Services;
using System.Collections.ObjectModel;

namespace Chrono;

public partial class ProjectsPage : ContentPage
{
    private readonly DatabaseService _databaseService;

    public ObservableCollection<Project> Projects { get; } = new();

    public ProjectsPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadProjectsAsync();
    }

    private async Task LoadProjectsAsync()
    {
        var projects = await _databaseService.GetProjectsAsync();
        Projects.Clear();
        foreach (var project in projects)
        {
            Projects.Add(project);
        }
        TotalProjectsLabel.Text = $"Total de proyectos: {Projects.Count}";
    }

    private async void OnNewProjectClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewProjectPage));
    }

    private async void OnProjectClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Project project)
        {
            await Shell.Current.GoToAsync($"{nameof(TasksPage)}?projectId={project.Id}");
        }
    }

    private async void OnEditProjectClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Project project)
        {
            await Shell.Current.GoToAsync($"{nameof(NewProjectPage)}?projectId={project.Id}");
        }
    }

    private async void OnDeleteProjectClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Project project)
        {
            bool confirm = await DisplayAlertAsync(
                "Eliminar Proyecto",
                $"¿Estás seguro de que deseas eliminar el proyecto '{project.Name}'? Esta acción eliminará permanentemente todas sus tareas y registros de tiempo asociados.",
                "Eliminar",
                "Cancelar");

            if (confirm)
            {
                await _databaseService.DeleteProjectAsync(project.Id);
                Projects.Remove(project);
                TotalProjectsLabel.Text = $"Total de proyectos: {Projects.Count}";
            }
        }
    }
}