using Chrono.Services;

namespace Chrono;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private async void OnProjectsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ProjectsPage));
    }
}