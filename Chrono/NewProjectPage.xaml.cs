using Chrono.Services;
using Chrono.Models;

namespace Chrono;

public partial class NewProjectPage : ContentPage
{
	private readonly DatabaseService _databaseService;
	public NewProjectPage(DatabaseService databaseService)
	{
		_databaseService = databaseService;
        InitializeComponent();
    }
	private async void OnSaveClicked(object sender, EventArgs e)
	{
		var project = new Project
		{
			Name = NameEntry.Text,
			Description = DescriptionEditor.Text
		};
		await _databaseService.SaveProjectAsync(project);
		await Shell.Current.GoToAsync("..");
	}
}