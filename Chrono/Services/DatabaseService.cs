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
}
