using System;
using System.ComponentModel;

namespace StratoDomainDDNSChanger.Core
{
    public class ConfigData : INotifyPropertyChanged
    {
        private string _websiteUrl = string.Empty;
        private string _password = string.Empty;
        private string _lastIPv4 = string.Empty;
        private string _lastIPv6 = string.Empty;
        private string _lastUpdated = string.Empty;
        private string _updateUrl = string.Empty;
        private string _userName = string.Empty;
        private string _getSelfIPv4Url = "https://api.ipify.org";
        private string _getSelfIPv6Url = string.Empty;
        private string _autorun = "1";
        private string _ignoreError = "1";
        private string _nextUpdate = "0";

        public string WebsiteUrl
        {
            get => _websiteUrl;
            set => SetProperty(ref _websiteUrl, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string LastIPv4
        {
            get => _lastIPv4;
            set => SetProperty(ref _lastIPv4, value);
        }

        public string LastIPv6
        {
            get => _lastIPv6;
            set => SetProperty(ref _lastIPv6, value);
        }

        public string LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public string UpdateUrl
        {
            get => _updateUrl;
            set => SetProperty(ref _updateUrl, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string GetSelfIPv4Url
        {
            get => _getSelfIPv4Url;
            set => SetProperty(ref _getSelfIPv4Url, value);
        }

        public string GetSelfIPv6Url
        {
            get => _getSelfIPv6Url;
            set => SetProperty(ref _getSelfIPv6Url, value);
        }

        public string Autorun
        {
            get => _autorun;
            set => SetProperty(ref _autorun, value);
        }

        public string IgnoreError
        {
            get => _ignoreError;
            set => SetProperty(ref _ignoreError, value);
        }

        public string NextUpdate
        {
            get => _nextUpdate;
            set => SetProperty(ref _nextUpdate, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Helper method to reduce redundancy
        protected virtual void SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}