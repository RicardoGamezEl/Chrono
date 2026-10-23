using System;
using Chrono.Models;
using Chrono.Pages.Tasks;
using Chrono.Services;

namespace Chrono;

public partial class MainPage : ContentPage
{
    private readonly StatsService _statsService;

    public MainPage(StatsService statsService)
    {
        InitializeComponent();
        _statsService = statsService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDashboardAsync();
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            var stats = await _statsService.GetDashboardStatsAsync();
            BindingContext = stats;

            EmptyProjectsView.IsVisible = !stats.HasProjects;
            ProjectsStack.IsVisible = stats.HasProjects;

            EmptyActivitiesView.IsVisible = !stats.HasActivities;
            RecentActivitiesStack.IsVisible = stats.HasActivities;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading dashboard: {ex.Message}");
        }
    }

    private async void OnProjectsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProjectsPage));
    }

    private async void OnNewProjectClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NewProjectPage));
    }

    private async void OnOpenProjectClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is ProjectProgressStats project)
        {
            await Shell.Current.GoToAsync($"{nameof(TasksPage)}?projectId={project.ProjectId}");
        }
    }
}