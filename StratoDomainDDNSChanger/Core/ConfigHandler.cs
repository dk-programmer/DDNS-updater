using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using StratoDomainDDNSChanger.MVVM.ViewModel;
using System.Text.Json;

namespace StratoDomainDDNSChanger.Core
{
    public class ConfigHandler
    {
        public static ConfigHandler Instance { get; set; }


        private static readonly string ConfigFilePath = "config.json";
        public ConfigData ConfigData { get; private set; }

        public ConfigHandler()
        {
            Instance = this;
            ConfigData = LoadConfig() ?? new ConfigData(); // Load existing config or create new one
        }

        // Save the configuration to a file using JSON serialization
        public void SaveConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(ConfigData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}");
            }
        }

        // Load the configuration from a file using JSON deserialization
        private ConfigData LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<ConfigData>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}");
            }
            return null;
        }

        // Reload the configuration, typically for a UI refresh
        public void ReloadConfig()
        {
            ConfigData = LoadConfig() ?? new ConfigData();
        }
    }
}