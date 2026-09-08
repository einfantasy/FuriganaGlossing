using System;

namespace FuriganaGlossing.Models
{
    public class AppConfig
    {
        public string OcrServerUrl { get; set; } = "http://localhost:5000";
        public string TranslationServerUrl { get; set; } = "http://localhost:5000";
        public string OcrModel { get; set; } = "general";
    }
}
