using System.Threading.Tasks;
using Chrono.Models;

namespace Chrono.Services;

public class StatsService
{
    private readonly DatabaseService _databaseService;
    private readonly ProductivityStatsCalculator _calculator;

    public StatsService(DatabaseService databaseService, ProductivityStatsCalculator calculator)
    {
        _databaseService = databaseService;
        _calculator = calculator;
    }

    public async Task<ProductivityDashboardStats> GetDashboardStatsAsync()
    {
        var projects = await _databaseService.GetProjectsAsync();
        var tasks = await _databaseService.GetAllTasksAsync();
        var timeEntries = await _databaseService.GetAllTimeEntriesAsync();

        return _calculator.CalculateDashboardStats(projects, tasks, timeEntries);
    }
}
