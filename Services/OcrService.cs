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
    }

    public class OcrService : IOcrService
    {
        private readonly IConfigService _configService;
        private readonly HttpClient _httpClient;

        public OcrService(IConfigService configService)
        {
            _configService = configService;
            _httpClient = new HttpClient();
        }

        public async Task<OcrResult> PerformOcrAsync(byte[] imageBytes)
        {
            var config = await _configService.LoadConfigAsync();
            var url = config.OcrServerUrl + "/v1/chat/completions";

            try
            {
                var base64Image = Convert.ToBase64String(imageBytes);
                
                var requestBody = new
                {
                    model = config.OcrModel,
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

                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                response.EnsureSuccessStatusCode();

                var llmResponse = await response.Content.ReadFromJsonAsync<LlmResponse>();
                var content = llmResponse?.Choices?.FirstOrDefault()?.Message?.Content;
                
                if (string.IsNullOrEmpty(content)) return new OcrResult();

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
                Console.WriteLine($"OCR Error: {ex.Message}");
                return new OcrResult();
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
