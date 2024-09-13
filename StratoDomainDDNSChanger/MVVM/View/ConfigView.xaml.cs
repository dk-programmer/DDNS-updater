using StratoDomainDDNSChanger.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StratoDomainDDNSChanger.MVVM.View
{
    /// <summary>
    /// Interaction logic for ConfigView.xaml
    /// </summary>
    public partial class ConfigView : UserControl
    {
        public ConfigView()
        {
            InitializeComponent();
            FillComponents();
        }
        public void FillComponents()
        {
            domainUrlTextBox.Text = ConfigHandler.Instance.ConfigData.WebsiteUrl;
            passText.Password = ConfigHandler.Instance.ConfigData.Password;
            updateUrl.Text = ConfigHandler.Instance.ConfigData.UpdateUrl;
            usernameTextBox.Text = ConfigHandler.Instance.ConfigData.UserName;
            autorun.IsChecked = (bool)StringToBoolConverter.ConvertToBool(ConfigHandler.Instance.ConfigData.Autorun, null, null, null);
            ignoreerrors.IsChecked = (bool)StringToBoolConverter.ConvertToBool(ConfigHandler.Instance.ConfigData.IgnoreError, null, null, null);
        }
        private void PasswordBoxControl_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ConfigHandler.Instance.ConfigData.Password = passText.Password;
            // Handle password input
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigHandler.Instance.ConfigData.WebsiteUrl = domainUrlTextBox.Text;
            ConfigHandler.Instance.ConfigData.UserName = usernameTextBox.Text;
            ConfigHandler.Instance.ConfigData.Password = passText.Password;
            ConfigHandler.Instance.ConfigData.UpdateUrl = updateUrl.Text;
            ConfigHandler.Instance.ConfigData.Autorun = (string)StringToBoolConverter.ConvertToString(autorun.IsChecked.Value, null, null, null);
            ConfigHandler.Instance.ConfigData.IgnoreError = (string)StringToBoolConverter.ConvertToString(ignoreerrors.IsChecked.Value, null, null, null);
            ConfigHandler.Instance.SaveConfig();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigHandler.Instance.ReloadConfig();
            FillComponents();
        }
    }
}
