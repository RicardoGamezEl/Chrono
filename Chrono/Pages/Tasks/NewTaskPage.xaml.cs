using Chrono.Services;
using Chrono.Models;

namespace Chrono.Pages.Tasks;

[QueryProperty(nameof(ProjectId), "projectId")]
public partial class NewTaskPage : ContentPage
{
	private readonly DatabaseService _databaseService;
	public string ProjectId { get; set; } = string.Empty;
    public NewTaskPage(DatabaseService databaseService)
	{
		_databaseService = databaseService;
        InitializeComponent();
	}

	public async void OnSaveClicked(object sender, EventArgs e)
	{
		var task = new TaskItem
		{
			ProjectId = int.Parse(ProjectId),
            Title = TitleEntry.Text,
			Description = DescriptionEditor.Text
		};
		await _databaseService.SaveTaskAsync(task);
        await Shell.Current.GoToAsync("..");
    }
}