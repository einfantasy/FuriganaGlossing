using System.Windows;
using FuriganaGlossing.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FuriganaGlossing.ViewModels;

namespace FuriganaGlossing
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        public static IProcessManagerService ProcessManager => ServiceProvider.GetRequiredService<IProcessManagerService>();
        public static ILogService LogService => ServiceProvider.GetRequiredService<ILogService>();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILogService, LogService>();
                    services.AddSingleton<IProcessManagerService, ProcessManagerService>();
                    services.AddSingleton<IConfigService, ConfigService>();
                    services.AddSingleton<IFuriganaService, FuriganaService>();
                    services.AddHttpClient();
                    services.AddTransient<IOcrService, OcrService>();
                    services.AddTransient<ITranslationService, TranslationService>();
                    services.AddTransient<MainViewModel>();
                })
                .Build();

            ServiceProvider = host.Services;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ProcessManager?.StopAll();
            base.OnExit(e);
        }
    }
}
