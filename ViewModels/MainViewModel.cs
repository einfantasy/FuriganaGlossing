using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Threading.Tasks;
using FuriganaGlossing.Services;
using FuriganaGlossing.Models;
using FuriganaGlossing.Views;
using System.Linq;
using System.Text;

namespace FuriganaGlossing.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IConfigService _configService;
        private readonly IOcrService _ocrService;
        private readonly IFuriganaService _furiganaService;
        private readonly ITranslationService _translationService;

        [ObservableProperty]
        private string _ocrText;

        [ObservableProperty]
        private string _translationText;

        [ObservableProperty]
        private OcrResult _ocrResults;

        [ObservableProperty]
        private List<FuriganaToken> _furiganaTokens;

        [ObservableProperty]
        private string _resultHtml;

        public ReadOnlyObservableCollection<string> Logs => App.LogService.Logs;

        public MainViewModel(IConfigService configService)
        {
            _configService = configService;
            _ocrService = new OcrService(configService);
            _furiganaService = new FuriganaService(configService);
            _translationService = new TranslationService(configService);
        }

        [RelayCommand]
        private void OpenSettings()
        {
            var configViewModel = new ConfigViewModel(_configService, App.ProcessManager);
            var configWindow = new ConfigWindow(configViewModel);
            
            configWindow.Owner = Application.Current.MainWindow;
            configWindow.Activated += async (s, e) => await configViewModel.InitializeAsync();
            configWindow.ShowDialog();
        }

        public async Task ProcessImageAsync(byte[] imageBytes)
        {
            // 1. Perform OCR
            OcrResults = await _ocrService.PerformOcrAsync(imageBytes);
            
            // Combine all lines into one text for processing
            string fullText = string.Join(Environment.NewLine, OcrResults.Lines.ConvertAll(l => l.Text));
            OcrText = fullText;

            // 2. Get Furigana
            FuriganaTokens = await _furiganaService.GetFuriganaAsync(fullText);

            // 3. Get Translation
            TranslationText = await _translationService.TranslateAsync(fullText);

            // 4. Generate HTML for WebView2
            ResultHtml = GenerateResultHtml();
        }

        private string GenerateResultHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<html><body style=\"font-family: 'Meiryo', 'MS PGothic', sans-serif; line-height: 2.2; padding: 60px 20px; background-color: #fdfdfd; display: flex; flex-direction: column; align-items: center; text-align: center;\">");
            
            // Japanese original text and furigana (Center, Large)
            sb.Append("<div style=\"font-size: 40px; color: #000; margin-bottom: 40px;\">");
            if (FuriganaTokens != null && FuriganaTokens.Any())
            {
                foreach (var token in FuriganaTokens)
                {
                    if (token.OriginalText == token.Reading)
                    {
                        sb.Append(token.OriginalText);
                    }
                    else
                    {
                        sb.Append($"<ruby>{token.OriginalText}<rt style=\"font-size: 0.5em; color: #888;\">{token.Reading}</rt></ruby>");
                    }
                }
            }
            else
            {
                sb.Append(OcrText);
            }
            sb.Append("</div>");

            // Translation (Below, Smaller)
            if (!string.IsNullOrEmpty(TranslationText))
            {
                sb.Append($"<div style=\"font-size: 20px; color: #333; max-width: 800px; font-weight: normal;\">{TranslationText}</div>");
            }
            
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
