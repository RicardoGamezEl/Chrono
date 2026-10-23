using Microsoft.Extensions.Logging;
using Chrono.Services;
using Chrono.Pages.Tasks;

namespace Chrono
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Services
            builder.Services.AddSingleton<DatabaseService>();

            // Pages
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ProjectsPage>();
            builder.Services.AddTransient<NewProjectPage>();
            builder.Services.AddTransient<TasksPage>();
            builder.Services.AddTransient<NewTaskPage>();
            builder.Services.AddTransient<TimerPage>();
            builder.Services.AddTransient<SessionHistoryPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
