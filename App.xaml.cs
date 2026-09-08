using System.Windows;
using FuriganaGlossing.Services;

namespace FuriganaGlossing
{
    public partial class App : Application
    {
    public static IProcessManagerService ProcessManager { get; private set; }
    public static ILogService LogService { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LogService = new LogService();
        ProcessManager = new ProcessManagerService();
    }

        protected override void OnExit(ExitEventArgs e)
        {
            ProcessManager?.StopAll();
            base.OnExit(e);
        }
    }
}
