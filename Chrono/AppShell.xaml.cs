namespace Chrono
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ProjectsPage), typeof(ProjectsPage));
        }
    }
}
