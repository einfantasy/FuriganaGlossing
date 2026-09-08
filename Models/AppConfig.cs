using System;

namespace FuriganaGlossing.Models
{
    public class AppConfig
    {
        public string OcrServerUrl { get; set; } = "http://localhost:5000";
        public string TranslationServerUrl { get; set; } = "http://localhost:5000";
        public string OcrStartCommand { get; set; } = string.Empty;
        public string LlmStartCommand { get; set; } = string.Empty;
    }
}
