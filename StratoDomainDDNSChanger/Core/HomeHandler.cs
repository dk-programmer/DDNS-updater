using StratoDomainDDNSChanger.MVVM.View;
using StratoDomainDDNSChanger.MVVM.ViewModel;
using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;

namespace StratoDomainDDNSChanger.Core
{
    class HomeHandler
    {
        public static HomeHandler Instance { get; set; }
        private static readonly HttpClient httpClient = CreateHttpClient();

        Task DDnsTask;
        Task GetIpTask;
        CancellationTokenSource ts = new CancellationTokenSource();

        public bool isIpChanged;
        public bool IsGetIpRunning
        {
            get { return GetIpTask != null; }
        }

        public bool IsDDnsUpdateRunning
        {
            get { return DDnsTask != null; }
        }

        public HomeHandler()
        {
            Instance = this;

            //do a later run on this because MainViewModel is initilized later;
            Task.Run(async () =>
            {
                while (MainViewModel.Instance == null)
                {
                    await Task.Delay(100); // Wait and retry
                }
                MainViewModel.Instance.PropertyChanged += OnPropertyChangedHandler;
                while (HomeView.Instance == null)
                {
                    await Task.Delay(100); // Wait and retry
                }
                UpdateAnimationStates();
            });
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DDNS-Updater/0.5");
            return client;
        }

        public void StartFetchIpAddresses()
        {
            if (!IsGetIpRunning)
            {
                HomeViewModel.Instance?.StartGetIPRunningImage(this, null);
                GetIpTask = Task.Run(() => FetchAndDisplayIpAddressesAsync());
            }
        }

        public void StartUpdateDDnsWithConfig()
        {
            string updateUrl = ConfigHandler.Instance.ConfigData.UpdateUrl;
            string hostname = ConfigHandler.Instance.ConfigData.WebsiteUrl;
            string username = ConfigHandler.Instance.ConfigData.UserName;
            string password = ConfigHandler.Instance.ConfigData.Password;

            string currentIPv4 = ConfigHandler.Instance.ConfigData.LastIPv4;
            string currentIPv6 = ConfigHandler.Instance.ConfigData.LastIPv6;

            HomeViewModel.Instance?.StartDDnsRunningImage(this, null);
            DDnsTask = Task.Run(() => UpdateDDNS(updateUrl, hostname, username, password, currentIPv4, currentIPv6));
        }

        public void StopAllActions()
        {
            ts.Cancel();
            ts = new CancellationTokenSource();
            DDnsTask = null;
            GetIpTask = null;

            HomeViewModel.Instance?.StopDDnsRunningImage(this, null);
            HomeViewModel.Instance?.StopGetIPRunningImage(this, null);
        }

        public void UpdateAnimationStates()
        {
            if (HomeHandler.Instance.IsGetIpRunning) HomeViewModel.Instance.StartGetIPRunningImage(this, null);
            else HomeViewModel.Instance.StopGetIPRunningImage(this, null);
            if (HomeHandler.Instance.IsDDnsUpdateRunning) HomeViewModel.Instance.StartDDnsRunningImage(this, null);
            else HomeViewModel.Instance.StopDDnsRunningImage(this, null);
        }

        private static string ResolveIpv4LookupUrl()
        {
            string url = ConfigHandler.Instance.ConfigData.GetSelfIPv4Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://api.ipify.org";
            }
            return url.Trim();
        }

        /// <summary>
        /// Returns null when IPv6 updates are disabled (empty GetSelfIPv6Url).
        /// </summary>
        private static string ResolveIpv6LookupUrlOrNull()
        {
            string url = ConfigHandler.Instance.ConfigData.GetSelfIPv6Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }
            return url.Trim();
        }

        private static bool TryNormalizeIp(string raw, bool wantIpv6, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string candidate = raw.Trim().Trim('[', ']');
            if (!IPAddress.TryParse(candidate, out IPAddress address))
            {
                return false;
            }

            if (wantIpv6)
            {
                if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6 || address.IsIPv6LinkLocal)
                {
                    return false;
                }
            }
            else if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return false;
            }

            normalized = address.ToString();
            return true;
        }

        async Task FetchAndDisplayIpAddressesAsync()
        {
            CancellationToken ct = ts.Token;
            while (true)
            {
                try
                {
                    // Fetch current IP addresses using configured lookup URLs
                    try
                    {
                        string rawV4 = await httpClient.GetStringAsync(ResolveIpv4LookupUrl());
                        if (TryNormalizeIp(rawV4, wantIpv6: false, out string currentIPv4)
                            && currentIPv4 != ConfigHandler.Instance.ConfigData.LastIPv4)
                        {
                            isIpChanged = true;
                            ConfigHandler.Instance.ConfigData.LastIPv4 = currentIPv4;
                        }
                    }
                    catch (Exception)
                    {
                        //TODO logging
                    }

                    try
                    {
                        string ipv6Lookup = ResolveIpv6LookupUrlOrNull();
                        if (ipv6Lookup == null)
                        {
                            // IPv6 publishing disabled — clear stale AAAA value so updates are IPv4-only
                            if (!string.IsNullOrWhiteSpace(ConfigHandler.Instance.ConfigData.LastIPv6))
                            {
                                isIpChanged = true;
                                ConfigHandler.Instance.ConfigData.LastIPv6 = string.Empty;
                            }
                        }
                        else
                        {
                            string rawV6 = await httpClient.GetStringAsync(ipv6Lookup);
                            if (TryNormalizeIp(rawV6, wantIpv6: true, out string currentIPv6)
                                && currentIPv6 != ConfigHandler.Instance.ConfigData.LastIPv6)
                            {
                                isIpChanged = true;
                                ConfigHandler.Instance.ConfigData.LastIPv6 = currentIPv6;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //TODO logging
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error while getting IP.\n" + ex.ToString());
                    GetIpTask = null;
                    HomeViewModel.Instance?.StopGetIPRunningImage(this, null);
                    break;
                }
                if (isIpChanged)
                {
                    ConfigHandler.Instance.ConfigData.LastUpdated = System.DateTime.Now.ToString("dd/MM/yy HH:mm") + " (ip has changed)";
                    ConfigHandler.Instance.SaveConfig();
                    StartUpdateDDnsWithConfig();
                }
                else
                {
                    ConfigHandler.Instance.ConfigData.LastUpdated = System.DateTime.Now.ToString("dd/MM/yy HH:mm") + " (no change)";
                    ConfigHandler.Instance.SaveConfig();
                }

                float waitSec = 5 * 60;
                while (waitSec > 0)
                {
                    await Task.Delay(1000, ct);
                    waitSec--;
                    ConfigHandler.Instance.ConfigData.NextUpdate = waitSec + "";
                }
                isIpChanged = false;
            }
        }

        async Task UpdateDDNS(string updateUrl, string hostname, string username, string password, string currentIPv4, string currentIPv6)
        {
            CancellationToken ct = ts.Token;
            try
            {
                if (string.IsNullOrWhiteSpace(currentIPv4) && string.IsNullOrWhiteSpace(currentIPv6))
                {
                    MessageBox.Show("No IP found.");
                    return;
                }

                // Properly encode URL parameters
                string encodedHostname = HttpUtility.UrlEncode(hostname);

                // Strato / DynDNS: myip can be IPv4, IPv6, or "IPv4,IPv6"
                string myIp;
                if (!string.IsNullOrWhiteSpace(currentIPv4) && !string.IsNullOrWhiteSpace(currentIPv6))
                {
                    myIp = $"{currentIPv4.Trim()},{currentIPv6.Trim()}";
                }
                else if (!string.IsNullOrWhiteSpace(currentIPv4))
                {
                    myIp = currentIPv4.Trim();
                }
                else
                {
                    myIp = currentIPv6.Trim();
                }

                string url = $"{updateUrl}?hostname={encodedHostname}&myip={HttpUtility.UrlEncode(myIp)}";

                // Create an instance of HttpClientHandler without bypassing SSL validation
                HttpClientHandler handler = new HttpClientHandler();

                // Use the handler with HttpClient
                using (HttpClient client = new HttpClient(handler))
                {
                    // Set the basic authentication header
                    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                    // Set the User-Agent header to mimic curl
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7.68.0");

                    try
                    {
                        // Send the request
                        HttpResponseMessage response = await client.GetAsync(url, ct);

                        // Ensure the request was successful
                        response.EnsureSuccessStatusCode();

                        // Read and display the response
                        string responseBody = await response.Content.ReadAsStringAsync();
                        ConfigHandler.Instance.ConfigData.LastUpdated = $"{System.DateTime.Now.ToString("dd/MM/yy HH:mm")} (ddns message {responseBody})";
                        ConfigHandler.Instance.SaveConfig();
                    }
                    catch (HttpRequestException e)
                    {
                        if (ConfigHandler.Instance.ConfigData.IgnoreError != "true")
                        {
                            // Display a generic error message to the user
                            MessageBox.Show(e.Message);
                            MessageBox.Show("An error occurred while updating the DNS. Please try again.");
                        }
                    }
                }
            }
            finally
            {
                DDnsTask = null;
                HomeViewModel.Instance?.StopDDnsRunningImage(this, null);
            }
        }

        private void OnPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.Instance.CurrentView))
            {
                StopAllActions();
            }
            UpdateAnimationStates();
        }
    }
}
