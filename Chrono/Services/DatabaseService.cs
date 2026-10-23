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

    // Time Tracking & Sessions
    public async Task SaveTimeEntryAsync(TimeEntry entry)
    {
        var db = await GetDatabaseAsync();
        await db.InsertAsync(entry);
    }

    public async Task UpdateTimeEntryAsync(TimeEntry entry)
    {
        var db = await GetDatabaseAsync();
        await db.UpdateAsync(entry);
    }

    public async Task DeleteTimeEntryAsync(int id)
    {
        var db = await GetDatabaseAsync();
        await db.Table<TimeEntry>().Where(te => te.Id == id).DeleteAsync();
    }

    public async Task<TimeEntry?> GetActiveTimeEntryAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TimeEntry>().FirstOrDefaultAsync(te => te.End == null);
    }

    public async Task<TimeEntry?> GetActiveTimeEntryByTaskAsync(int taskId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TimeEntry>().FirstOrDefaultAsync(te => te.TaskId == taskId && te.End == null);
    }

    public async Task<List<TimeEntry>> GetTimeEntriesByTaskAsync(int taskId)
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TimeEntry>()
            .Where(te => te.TaskId == taskId)
            .OrderByDescending(te => te.Start)
            .ToListAsync();
    }

    public async Task<List<TimeEntry>> GetTimeEntriesByProjectAsync(int projectId)
    {
        var db = await GetDatabaseAsync();
        var tasks = await db.Table<TaskItem>().Where(t => t.ProjectId == projectId).ToListAsync();
        if (tasks.Count == 0)
            return new List<TimeEntry>();

        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var allEntries = await db.Table<TimeEntry>().ToListAsync();
        return allEntries
            .Where(te => taskIds.Contains(te.TaskId))
            .OrderByDescending(te => te.Start)
            .ToList();
    }

    public async Task<TimeSpan> GetTotalDurationByTaskAsync(int taskId)
    {
        var entries = await GetTimeEntriesByTaskAsync(taskId);
        var total = TimeSpan.Zero;
        foreach (var entry in entries)
        {
            total += entry.Duration;
        }
        return total;
    }

    public async Task<TimeSpan> GetTotalDurationByProjectAsync(int projectId)
    {
        var entries = await GetTimeEntriesByProjectAsync(projectId);
        var total = TimeSpan.Zero;
        foreach (var entry in entries)
        {
            total += entry.Duration;
        }
        return total;
    }

    public async Task<List<TaskItem>> GetAllTasksAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TaskItem>().ToListAsync();
    }

    public async Task<List<TimeEntry>> GetAllTimeEntriesAsync()
    {
        var db = await GetDatabaseAsync();
        return await db.Table<TimeEntry>().ToListAsync();
    }
}
