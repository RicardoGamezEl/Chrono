using Chrono.Models;
using Chrono.Services;

namespace Chrono;

[QueryProperty(nameof(ProjectId), "projectId")]
public partial class NewProjectPage : ContentPage
{
    private readonly DatabaseService _databaseService;
    private Project? _existingProject;

    public string? ProjectId { get; set; }

    public NewProjectPage(DatabaseService databaseService)
    {
        _databaseService = databaseService;
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrEmpty(ProjectId) && int.TryParse(ProjectId, out int id) && id > 0)
        {
            _existingProject = await _databaseService.GetProjectByIdAsync(id);
            if (_existingProject != null)
            {
                Title = "Editar Proyecto";
                PageHeaderLabel.Text = "Editar Proyecto";
                NameEntry.Text = _existingProject.Name;
                DescriptionEditor.Text = _existingProject.Description;
                SaveButton.Text = "Actualizar";
                return;
            }
        }

        Title = "Nuevo Proyecto";
        PageHeaderLabel.Text = "Nuevo Proyecto";
        SaveButton.Text = "Guardar";
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Validación", "El nombre del proyecto es obligatorio.", "Aceptar");
            return;
        }

        if (_existingProject != null)
        {
            _existingProject.Name = name;
            _existingProject.Description = DescriptionEditor.Text?.Trim() ?? string.Empty;
            await _databaseService.UpdateProjectAsync(_existingProject);
        }
        else
        {
            var project = new Project
            {
                Name = name,
                Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now
            };
            await _databaseService.SaveProjectAsync(project);
        }

        await Shell.Current.GoToAsync("..");
    }
}