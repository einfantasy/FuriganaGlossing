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
                    services.AddTransient<ConfigViewModel>();
                })
                .Build();

            ServiceProvider = host.Services;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            ServiceProvider.GetRequiredService<IProcessManagerService>().StopAll();
            base.OnExit(e);
        }
    }
}
