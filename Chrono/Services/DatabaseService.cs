using SQLite;
using Chrono.Models;

namespace Chrono.Services;
public class DatabaseService
{
    private SQLiteAsyncConnection _database;
    public DatabaseService()
    {
        _database = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "chrono.db"));
    }
    public async Task InitializeAsync()
    {
        await _database.CreateTableAsync<Project>();
        await _database.CreateTableAsync<TaskItem>();
        await _database.CreateTableAsync<TimeEntry>();
    }
    public async Task SaveProjectAsync(Project project)
    {
        await _database.InsertAsync(project);
    }
    public async Task<List<Project>> GetProjectsAsync()
    {
        return await _database.Table<Project>().ToListAsync();
    }
    public async Task<List<TaskItem>> GetTaskByProjectAsync(int projectId)
    {
        return await _database
            .Table<TaskItem>()
            .Where(task => task.ProjectId == projectId)
            .ToListAsync();
    }

    public async Task SaveTaskAsync(TaskItem task)
    {
        await _database.InsertAsync(task);
    }
    /*
     * Eliminar tarea
     * public async Task DeleteTaskAsync(int taskId)
    {
        await _database
            .Table<TaskItem>()
            .Where(task => task.Id == taskId)
            .DeleteAsync();
    }*/
}
