using Chrono.Pages;
using Chrono.Pages.Tasks;

namespace Chrono
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ProjectsPage), typeof(ProjectsPage));
            Routing.RegisterRoute(nameof(NewProjectPage), typeof(NewProjectPage));
            Routing.RegisterRoute(nameof(TasksPage), typeof(TasksPage));
            Routing.RegisterRoute(nameof(NewTaskPage), typeof(NewTaskPage));
        }
    }
}
