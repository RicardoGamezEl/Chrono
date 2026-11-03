using Chrono.Services;

namespace Chrono
{
    public partial class App : Application
    {
        private readonly DatabaseService _databaseService;
        private readonly ITaskTimerService _timerService;

        public App(DatabaseService databaseService, ITaskTimerService timerService)
        {
            _databaseService = databaseService;
            _timerService = timerService;
            InitializeComponent();
            _ = InitializeAppAsync();
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                await _databaseService.InitializeAsync();
                await _timerService.ReconcileActiveSessionAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing app: {ex.Message}");
            }
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());

            window.Resumed += async (s, e) =>
            {
                try
                {
                    await _timerService.ReconcileActiveSessionAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error on window resume: {ex.Message}");
                }
            };

            window.Activated += async (s, e) =>
            {
                try
                {
                    await _timerService.ReconcileActiveSessionAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error on window activated: {ex.Message}");
                }
            };

            return window;
        }
    }
}