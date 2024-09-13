using StratoDomainDDNSChanger.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace StratoDomainDDNSChanger
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Event that handles the startup logic
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ConfigHandler configHandler = new ConfigHandler();
            HomeHandler homeHandler = new HomeHandler();


            configHandler.ReloadConfig();

            if(configHandler.ConfigData.Autorun == "true")
            {
                homeHandler.StartFetchIpAddresses();
            }
        }
    }
}
