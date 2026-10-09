using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using FuriganaGlossing.Models;

namespace FuriganaGlossing.Services
{
    public interface IConfigService
    {
        Task<AppConfig> LoadConfigAsync();
        Task SaveConfigAsync(AppConfig config);
    }

    public class ConfigService : IConfigService
    {
        private readonly string _configFilePath;

        public ConfigService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string folderPath = Path.Combine(appDataPath, "FuriganaGlossing");
            
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _configFilePath = Path.Combine(folderPath, "config.json");
        }

        public async Task<AppConfig> LoadConfigAsync()
        {
            if (!File.Exists(_configFilePath))
            {
                return new AppConfig();
            }

            try
            {
                string json = await File.ReadAllTextAsync(_configFilePath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public async Task SaveConfigAsync(AppConfig config)
        {
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_configFilePath, json);
        }
    }
}
