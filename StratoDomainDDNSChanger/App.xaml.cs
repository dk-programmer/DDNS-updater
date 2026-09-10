using System;
using System.Security.Principal;
using System.Threading;
using System.Windows;
using StratoDomainDDNSChanger.Core;
namespace StratoDomainDDNSChanger
{
    public partial class App : Application
    {
        private Mutex instance;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            bool created;
            instance = new Mutex(true, @"Local\DDNS-Updater-" + WindowsIdentity.GetCurrent().User.Value, out created);
            if (!created) { MessageBox.Show("DDNS Updater is already running."); Shutdown(); return; }
            try { new MainWindow(new ConfigStore()).Show(); }
            catch (Exception) { MessageBox.Show("Unable to load settings from " + ConfigStore.DefaultPath + ". The password can only be read by the Windows user who saved it.", "DDNS Updater"); Shutdown(); }
        }
        protected override void OnExit(ExitEventArgs e) { instance?.Dispose(); base.OnExit(e); }
    }
}
