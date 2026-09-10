using System;
using System.IO;
using System.Web.Script.Serialization;
using System.Windows;

namespace StratoDomainDDNSChanger.Core
{
    public class ConfigHandler
    {
        public static ConfigHandler Instance { get; set; }

        private static readonly string ConfigFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public ConfigData ConfigData { get; private set; }

        public ConfigHandler()
        {
            Instance = this;
            ConfigData = LoadConfig() ?? new ConfigData();
            EnsureLookupUrlDefaults();
        }

        private void EnsureLookupUrlDefaults()
        {
            if (ConfigData == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(ConfigData.GetSelfIPv4Url))
            {
                ConfigData.GetSelfIPv4Url = "https://api.ipify.org";
            }
            if (string.IsNullOrWhiteSpace(ConfigData.GetSelfIPv6Url))
            {
                // Empty = do not publish AAAA (IPv4-only DynDNS). Set to https://api6.ipify.org to enable.
                ConfigData.GetSelfIPv6Url = string.Empty;
            }
        }

        public void SaveConfig()
        {
            try
            {
                string json = Serializer.Serialize(ConfigData);
                // Pretty-print lightly for readability
                json = FormatJson(json);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save configuration: {ex.Message}");
            }
        }

        private ConfigData LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return Serializer.Deserialize<ConfigData>(json);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load configuration: {ex.Message}");
            }
            return null;
        }

        public void ReloadConfig()
        {
            ConfigData = LoadConfig() ?? new ConfigData();
            EnsureLookupUrlDefaults();
        }

        private static string FormatJson(string json)
        {
            try
            {
                var obj = Serializer.DeserializeObject(json);
                return Serializer.Serialize(obj);
            }
            catch
            {
                return json;
            }
        }
    }
}
