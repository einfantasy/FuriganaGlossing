using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FuriganaGlossing.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;

namespace FuriganaGlossing.Services
{
    public interface IOcrService
    {
        Task<OcrResult> PerformOcrAsync(byte[] imageBytes);
        Task<bool> CheckConnectionAsync();
    }


    public class OcrService : IOcrService
    {
        private readonly IConfigService _configService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogService _logService;

        public OcrService(IConfigService configService, IHttpClientFactory httpClientFactory, ILogService logService)
        {
            _configService = configService;
            _httpClientFactory = httpClientFactory;
            _logService = logService;
        }

        public async Task<OcrResult> PerformOcrAsync(byte[] imageBytes)
        {
            var config = await _configService.LoadConfigAsync();
            var url = config.OcrServerUrl + "/v1/chat/completions";
            _logService.Log($"Starting OCR request to {url}...", LogLevel.Info);

            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                var base64Image = Convert.ToBase64String(imageBytes);
                
                var requestBody = new
                {
                    model = "general",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = new object[]
                            {
                                new { type = "text", text = "Please perform OCR on this Japanese image and return only the recognized text." },
                                new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } }
                            }
                        }
                    }
                };

                    var response = await httpClient.PostAsJsonAsync(url, requestBody);

                response.EnsureSuccessStatusCode();

                var llmResponse = await response.Content.ReadFromJsonAsync<LlmResponse>();
                var content = llmResponse?.Choices?.FirstOrDefault()?.Message?.Content;
                
                if (string.IsNullOrEmpty(content)) 
                {
                    _logService.Log("OCR returned empty content", LogLevel.Warning);
                    return new OcrResult();
                }

                _logService.Log("OCR completed successfully", LogLevel.Info);
                return new OcrResult
                {
                    Lines = new List<OcrTextLine> 
                    { 
                        new OcrTextLine { Text = content, BoundingBox = new Rect() } 
                    }
                };
            }
            catch (Exception ex)
            {
                _logService.Log($"OCR Error: {ex.Message}", LogLevel.Error);
                return new OcrResult();
            }
        }

        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                var config = await _configService.LoadConfigAsync();
                var url = config.OcrServerUrl + "/v1/models";
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
            public List<LlmChoice>? Choices { get; set; }
        }

        private class LlmChoice
        {
            [JsonPropertyName("message")]
            public LlmMessage? Message { get; set; }
        }

        private class LlmMessage
        {
            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }
    }
}
