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

        [ObservableProperty]
        private string _ocrServerUrl = string.Empty;

        [ObservableProperty]
        private string _ocrModel = string.Empty;

        [ObservableProperty]
        private string _translationServerUrl = string.Empty;

        public ConfigViewModel(IConfigService configService)
        {
            _configService = configService;
        }

        public async Task InitializeAsync()
        {
            var config = await _configService.LoadConfigAsync();
            OcrServerUrl = config.OcrServerUrl;
            OcrModel = config.OcrModel;
            TranslationServerUrl = config.TranslationServerUrl;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                config.OcrServerUrl = OcrServerUrl;
                config.OcrModel = OcrModel;
                config.TranslationServerUrl = TranslationServerUrl;
                await _configService.SaveConfigAsync(config);
                WeakReferenceMessenger.Default.Send(new SaveCompletedMessage(true));
            }
            catch
            {
                WeakReferenceMessenger.Default.Send(new SaveCompletedMessage(false));
            }
        }
    }
}
