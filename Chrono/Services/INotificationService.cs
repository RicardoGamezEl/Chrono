using System;

namespace Chrono.Services;

public interface INotificationService
{
    void ShowToast(string title, string message, int durationSeconds = 4);
    event Action<string, string>? NotificationReceived;
}
