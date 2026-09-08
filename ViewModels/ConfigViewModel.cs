using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FuriganaGlossing.Models;
using FuriganaGlossing.Services;
using FuriganaGlossing.Messages;

namespace FuriganaGlossing.ViewModels
{
    public partial class ConfigViewModel : ObservableObject
    {
        private readonly IConfigService _configService;
        private readonly IProcessManagerService _processManagerService;

         [ObservableProperty]
         private string _ocrServerUrl = string.Empty;
 
         [ObservableProperty]
         private string _translationServerUrl = string.Empty;

        [ObservableProperty]
        private string _ocrStartCommand = string.Empty;

        [ObservableProperty]
        private string _llmStartCommand = string.Empty;

        public ConfigViewModel(IConfigService configService, IProcessManagerService processManagerService)
        {
            _configService = configService;
            _processManagerService = processManagerService;
        }

        public async Task InitializeAsync()
        {
            var config = await _configService.LoadConfigAsync();
            OcrServerUrl = config.OcrServerUrl;
            TranslationServerUrl = config.TranslationServerUrl;
            OcrStartCommand = config.OcrStartCommand;
            LlmStartCommand = config.LlmStartCommand;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                config.OcrServerUrl = OcrServerUrl;
                config.TranslationServerUrl = TranslationServerUrl;
                config.OcrStartCommand = OcrStartCommand;
                config.LlmStartCommand = LlmStartCommand;
                await _configService.SaveConfigAsync(config);
                WeakReferenceMessenger.Default.Send(new SaveCompletedMessage(true));
            }
            catch
            {
                WeakReferenceMessenger.Default.Send(new SaveCompletedMessage(false));
            }
        }

        [RelayCommand]
        private void StartServers()
        {
            _processManagerService.StartProcess(OcrStartCommand);
            _processManagerService.StartProcess(LlmStartCommand);
            WeakReferenceMessenger.Default.Send(new CloseConfigWindowMessage());
        }
    }
}
