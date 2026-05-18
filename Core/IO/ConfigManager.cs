using System.Security.Cryptography;
using System.Text.Json;
using Fcry.Core.Models;

namespace Fcry.Core.IO;

public static class ConfigManager
{
    private static string ConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Fcry",
        "config.json");

    public static AppConfig LoadOrCreate()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config?.ArgonSalt?.Length == 32)
                    return config;
            }
        }
        catch { }

        var newConfig = new AppConfig
        {
            ArgonSalt = RandomNumberGenerator.GetBytes(32)
        };
        Save(newConfig);
        return newConfig;
    }

    public static void Save(AppConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(ConfigPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
