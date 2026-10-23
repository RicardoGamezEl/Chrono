using SQLite;
using Chrono.Models;

namespace Chrono.Services;

public class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _isInitialized;

    public DatabaseService()
    {
        _database = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "chrono.db"));
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
            return;

        await _initLock.WaitAsync();
        try
        {
            if (!_isInitialized)
            {
                await _database.CreateTableAsync<Project>();
                await _database.CreateTableAsync<TaskItem>();
                await _database.CreateTableAsync<TimeEntry>();
                _isInitialized = true;
            }
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<SQLiteAsyncConnection> GetDatabaseAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }
        return _database;
    }

    // Projects
    public async Task<List<Project>> GetProjectsAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<Project>().OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<Project?> GetProjectByIdAsync(int id)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<Project>().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task SaveProjectAsync(Project project)
    {
        var db = await GetDatabaseAsync();
        await db.InsertAsync(project);
    }

    public async Task UpdateProjectAsync(Project project)
    {
        var db = await GetDatabaseAsync();
        await db.UpdateAsync(project);
    }

    public async Task DeleteProjectAsync(int projectId)
    {
        var db = await GetDatabaseAsync();
        var tasks = await db.Table<TaskItem>().Where(task => task.ProjectId == projectId).ToListAsync();
        foreach (var task in tasks)
        {
            await db.Table<TimeEntry>().Where(te => te.TaskId == task.Id).DeleteAsync();
            await db.DeleteAsync(task);
        }
        await db.Table<Project>().Where(p => p.Id == projectId).DeleteAsync();
    }

    // Tasks
    public async Task<List<TaskItem>> GetTaskByProjectAsync(int projectId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TaskItem>()
            .Where(task => task.ProjectId == projectId)
            .OrderByDescending(task => task.CreatedAt)
            .ToListAsync();
    }

    public async Task<TaskItem?> GetTaskByIdAsync(int taskId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TaskItem>().FirstOrDefaultAsync(task => task.Id == taskId);
    }

    public async Task SaveTaskAsync(TaskItem task)
    {
        var db = await GetDatabaseAsync();
        await db.InsertAsync(task);
    }

    public async Task UpdateTaskAsync(TaskItem task)
    {
        var db = await GetDatabaseAsync();
        await db.UpdateAsync(task);
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        var db = await GetDatabaseAsync();
        await db.Table<TimeEntry>().Where(te => te.TaskId == taskId).DeleteAsync();
        await db.Table<TaskItem>().Where(task => task.Id == taskId).DeleteAsync();
    }
}
