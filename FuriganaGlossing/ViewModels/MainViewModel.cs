using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FuriganaGlossing.Messages;
using FuriganaGlossing.Models;
using FuriganaGlossing.Services;
using System.Text;

namespace FuriganaGlossing.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IOcrService _ocrService;
        private readonly IFuriganaService _furiganaService;
        private readonly ITranslationService _translationService;
        private readonly ILogService _logService;

        [ObservableProperty]
        private string _ocrText = string.Empty;

        [ObservableProperty]
        private string _translationText = string.Empty;

        [ObservableProperty]
        private OcrResult _ocrResults = new();

        [ObservableProperty]
        private List<FuriganaToken> _furiganaTokens = new();

        [ObservableProperty]
        private string _resultHtml = string.Empty;

        [ObservableProperty]
        private bool _isOcrServerConnected;

        [ObservableProperty]
        private bool _isTranslationServerConnected;
        [ObservableProperty]
        private bool _isProcessing;

        public MainViewModel(IOcrService ocrService, IFuriganaService furiganaService, ITranslationService translationService, ILogService logService)
        {
            _ocrService = ocrService;
            _furiganaService = furiganaService;
            _translationService = translationService;
            _logService = logService;

            _ = StartConnectionCheckTimer();
        }

        private async Task StartConnectionCheckTimer()
        {
            while (true)
            {
                await UpdateConnectionStatusAsync();
                await Task.Delay(TimeSpan.FromSeconds(4));
            }
        }

        public async Task UpdateConnectionStatusAsync()
        {
            IsOcrServerConnected = await _ocrService.CheckConnectionAsync();
            IsTranslationServerConnected = await _translationService.CheckConnectionAsync();
        }

        [RelayCommand]
        private void OpenSettings()
        {
            WeakReferenceMessenger.Default.Send(new OpenSettingsRequestedMessage());
        }

        public async Task ProcessImageAsync(byte[] imageBytes)
        {
            IsProcessing = true;
            try
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
            catch (Exception ex)
            {
                _logService.Log($"Image processing failed: {ex.Message}", LogLevel.Error);
                throw;
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private string GenerateResultHtml()
        {
            var sb = new StringBuilder();
            sb.Append("<html><body style=\"font-family: 'Meiryo', 'MS PGothic', sans-serif; line-height: 2.2; padding: 60px 20px; background-color: #fdfdfd; display: flex; flex-direction: column; align-items: center; text-align: center;\">");
            
            // Japanese original text and furigana (Center, Large)
            sb.Append("<div style=\"font-size: 40px; color: #000; margin-bottom: 40px;\">");
            if (FuriganaTokens.Any())
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
                sb.Append($"<div style=\"font-size: 20px; color: #333; max-width: 800px; font-weight: normal; font-family: 'Google Sans';\">{TranslationText}</div>");
            }
            
            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}
