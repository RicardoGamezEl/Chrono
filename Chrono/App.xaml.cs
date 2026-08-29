using Chrono.Services;

namespace Chrono
{
    public partial class App : Application
    {
        private readonly DatabaseService _databaseService;
        public App(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            InitializeComponent();
            _ = InitializeDatabaseAsync();
        }
        private async Task InitializeDatabaseAsync()
        {
            await _databaseService.InitializeAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}