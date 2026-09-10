using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FuriganaGlossing.Models;
using System.Text.Json.Serialization;
using System.Linq;

namespace FuriganaGlossing.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(string text);
        Task<bool> CheckConnectionAsync();
    }


    public class TranslationService : ITranslationService
    {
        private readonly IConfigService _configService;
        private readonly IHttpClientFactory _httpClientFactory;

        public TranslationService(IConfigService configService, IHttpClientFactory httpClientFactory)
        {
            _configService = configService;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> TranslateAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var config = await _configService.LoadConfigAsync();
            var url = config.TranslationServerUrl + "/v1/chat/completions";
            App.LogService.Log($"Requesting translation from {url}...", LogLevel.Info);

            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                var requestBody = new
                {
                    model = "gpt-4o", // Default model, could be moved to config
                    messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content = "You are a professional Japanese to Chinese translator. Translate the given Japanese text into natural, fluent Chinese."
                        },
                        new
                        {
                            role = "user",
                            content = text
                        }
                    }
                };

                    var response = await httpClient.PostAsJsonAsync(url, requestBody);

                response.EnsureSuccessStatusCode();

                var llmResponse = await response.Content.ReadFromJsonAsync<LlmResponse>();
                var result = llmResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
                
                App.LogService.Log("Translation completed successfully", LogLevel.Info);
                return result;
            }
            catch (Exception ex)
            {
                App.LogService.Log($"Translation Error: {ex.Message}", LogLevel.Error);
                return "Translation failed.";
            }
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                var url = config.TranslationServerUrl + "/v1/models";
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class LlmResponse
        {
            [JsonPropertyName("choices")]
            public List<LlmChoice> Choices { get; set; }
        }

        private class LlmChoice
        {
            [JsonPropertyName("message")]
            public LlmMessage Message { get; set; }
        }

        private class LlmMessage
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }
    }
}
