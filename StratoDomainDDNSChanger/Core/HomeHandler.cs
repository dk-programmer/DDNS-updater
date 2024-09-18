using StratoDomainDDNSChanger.MVVM.View;
using StratoDomainDDNSChanger.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
        private static readonly HttpClient httpClient = new HttpClient();

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

        async Task FetchAndDisplayIpAddressesAsync()
        {
            CancellationToken ct = ts.Token;
            while (true)
            {
                try
                {
                    // Fetch current IP addresses
                    try
                    {
                        string currentIPv4 = await httpClient.GetStringAsync("https://api.ipify.org");
                        if (currentIPv4 != ConfigHandler.Instance.ConfigData.LastIPv4)
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
                        string currentIPv6 = await httpClient.GetStringAsync("https://api6.ipify.org");
                        if (currentIPv6 != ConfigHandler.Instance.ConfigData.LastIPv6)
                        {
                            isIpChanged = true;
                            ConfigHandler.Instance.ConfigData.LastIPv6 = currentIPv6;
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
                    StartUpdateDDnsWithConfig();
                }
                else
                {
                    ConfigHandler.Instance.ConfigData.LastUpdated = System.DateTime.Now.ToString("dd/MM/yy HH:mm") + " (no change)";
                }

                float waitSec = 5 * 60;
                while (waitSec > 0)
                {
                    await Task.Delay(1000, ct);
                    waitSec--;
                    ConfigHandler.Instance.ConfigData.NextUpdate = waitSec +"";
                }
                isIpChanged = false;
            }
        }

        async Task UpdateDDNS(string updateUrl, string hostname, string username, string password, string currentIPv4, string currentIPv6)
        {
            CancellationToken ct = ts.Token;
            if (string.IsNullOrWhiteSpace(currentIPv4) && string.IsNullOrWhiteSpace(currentIPv6))
            {
                MessageBox.Show("No IP found.");
                return;
            }

            // Properly encode URL parameters
            string encodedHostname = HttpUtility.UrlEncode(hostname);
            string encodedIPv4 = !string.IsNullOrWhiteSpace(currentIPv4) ? HttpUtility.UrlEncode(currentIPv4) : "";
            string encodedIPv6 = !string.IsNullOrWhiteSpace(currentIPv6) ? HttpUtility.UrlEncode(currentIPv6) : "";

            // Construct the URL with encoded parameters
            string url = $"{updateUrl}?hostname={encodedHostname}";
            if (!string.IsNullOrWhiteSpace(encodedIPv4))
            {
                url += $"&myip={encodedIPv4}";
            }
            if (!string.IsNullOrWhiteSpace(encodedIPv6))
            {
                url += $",{encodedIPv6}";
            }

            //MessageBox.Show(url); //<- Enable to get a message before update.

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
                    HttpResponseMessage response = await client.GetAsync(url,ct);

                    // Ensure the request was successful
                    response.EnsureSuccessStatusCode();

                    // Read and display the response
                    string responseBody = await response.Content.ReadAsStringAsync();
                    ConfigHandler.Instance.ConfigData.LastUpdated = $"{ System.DateTime.Now.ToString("dd/MM/yy HH:mm")} (ddns message {responseBody}";
                }
                catch (HttpRequestException e)
                {
                    if(ConfigHandler.Instance.ConfigData.IgnoreError != "true")
                    {
                        // Display a generic error message to the user
                        MessageBox.Show(e.Message);
                        MessageBox.Show("An error occurred while updating the DNS. Please try again.");

                    }
                }
            }

            HomeViewModel.Instance?.StopDDnsRunningImage(this, null);
        }

        private void OnPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(MainViewModel.Instance.CurrentView))
            {
                StopAllActions();
            } 
            UpdateAnimationStates();
        }
    }
}
