using System.IO;
using DittoMeOff.Models;
using Newtonsoft.Json;

namespace DittoMeOff.Services;

public class ConfigService
{
    private readonly string _configPath;
    private AppConfig _config;

    public AppConfig Config => _config;

    public ConfigService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DittoMe-Off"
        );
        
        Directory.CreateDirectory(appDataPath);
        _configPath = Path.Combine(appDataPath, "config.json");
        _config = Load();
    }

    private AppConfig Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonConvert.DeserializeObject<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
        }
        
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
        }
    }

    public void UpdateConfig(Action<AppConfig> updateAction)
    {
        updateAction(_config);
        Save();
    }
}
