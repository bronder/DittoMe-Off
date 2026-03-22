using System.IO;
using DittoMeOff.Models;
using Newtonsoft.Json;
using NLog;

namespace DittoMeOff.Services;

public class ConfigService : IConfigService
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly string _configPath;
    private AppConfig _config;

    public AppConfig Config => _config;

    public ConfigService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppConstants.DatabaseFolderName
        );
        
        Directory.CreateDirectory(appDataPath);
        _configPath = Path.Combine(appDataPath, AppConstants.ConfigFileName);
        _config = Load();
        
        _logger.Info("Configuration loaded from {ConfigPath}", _configPath);
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
            _logger.Error(ex, "Error loading config");
        }
        
        return new AppConfig();
    }

    public void Save()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
            File.WriteAllText(_configPath, json);
            _logger.Debug("Configuration saved");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error saving config");
        }
    }

    public void UpdateConfig(Action<AppConfig> updateAction)
    {
        updateAction(_config);
        Save();
    }
}
