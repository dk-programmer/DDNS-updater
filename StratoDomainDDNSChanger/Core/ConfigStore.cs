using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
namespace StratoDomainDDNSChanger.Core
{
    public sealed class ConfigStore
    {
        public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DDNS Updater", "config.json");
        public string FilePath { get; }
        public ConfigStore(string path = null) { FilePath = path ?? DefaultPath; }
        public Settings Load()
        {
            if (!File.Exists(FilePath)) return new Settings();
            var settings = new JavaScriptSerializer().Deserialize<Settings>(File.ReadAllText(FilePath));
            if (settings == null || settings.Version != 1) throw new InvalidDataException("Unsupported settings version.");
            if (!string.IsNullOrEmpty(settings.ProtectedPassword))
                settings.Password = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(settings.ProtectedPassword), null, DataProtectionScope.CurrentUser));
            return settings;
        }
        public void Save(Settings settings)
        {
            settings.Validate();
            settings.ProtectedPassword = Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(settings.Password), null, DataProtectionScope.CurrentUser));
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string temporary = FilePath + ".tmp";
            try
            {
                File.WriteAllText(temporary, new JavaScriptSerializer().Serialize(settings), new UTF8Encoding(false));
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
    }
}
