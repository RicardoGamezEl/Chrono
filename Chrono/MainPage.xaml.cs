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
    private async void OnCreateProjectClicked(object sender, EventArgs e)
    {
    }

}