using System;
using System.Threading.Tasks;
using Chrono.Models;

namespace Chrono.Services;

public interface ICountdownSessionService
{
    ActiveSessionState? CurrentSession { get; }
    bool HasActiveSession { get; }

    event EventHandler<ActiveSessionState>? TimerTick;
    event EventHandler<ActiveSessionState>? SessionCompleted;
    event EventHandler<ActiveSessionState>? SessionStarted;

    Task<ActiveSessionState?> GetActiveSessionStateAsync(DateTime? referenceNow = null);
    Task<TimeSpan> CalculateRemainingTimeAsync(int taskId, TimeSpan? currentSessionElapsed = null);
    Task<ActiveSessionState> StartSessionAsync(int taskId, DateTime? referenceStartTime = null);
    Task<ActiveSessionState?> StopSessionAsync(int? taskId = null, DateTime? referenceEndTime = null);
    Task<ActiveSessionState?> ReconcileActiveSessionAsync(DateTime? referenceNow = null);
}
