using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using StratoDomainDDNSChanger.Core;
namespace StratoDomainDDNSChanger
{
    public sealed class HostStatusRow : INotifyPropertyChanged
    {
        public string Hostname { get; set; }
        private string message;
        public string Message { get => message; set { message = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Message))); } }
        public event PropertyChangedEventHandler PropertyChanged;
    }
    public partial class MainWindow : Window
    {
        private readonly ConfigStore store;
        private Settings settings;
        private readonly ObservableCollection<HostEntry> hosts = new ObservableCollection<HostEntry>();
        private readonly ObservableCollection<HostStatusRow> statuses = new ObservableCollection<HostStatusRow>();
        private UpdateService service;
        private CancellationTokenSource cancellation;
        private Task monitoring, manualCheck;
        private bool closing;
        public MainWindow(ConfigStore store, Settings initialSettings = null)
        {
            InitializeComponent();
            this.store = store;
            ModeColumn.ItemsSource = Enum.GetValues(typeof(AddressMode));
            HostsGrid.ItemsSource = hosts; StatusList.ItemsSource = statuses;
            settings = initialSettings ?? store.Load();
            EndpointBox.Text = settings.UpdateUrl; UsernameBox.Text = settings.Username; PasswordInput.Password = settings.Password;
            IPv4Box.Text = settings.IPv4LookupUrl; IPv6Box.Text = settings.IPv6LookupUrl; AutoStartBox.IsChecked = settings.AutoStart;
            foreach (var host in settings.Hosts) hosts.Add(new HostEntry { Hostname = host.Hostname, Mode = host.Mode });
            SaveStatus.Text = "Settings: " + store.FilePath;
            Loaded += (s, e) => { if (settings.AutoStart && settings.Hosts.Count > 0) Start(); };
        }
        private bool EnsureService()
        {
            if (service != null) return true;
            try
            {
                service = new UpdateService(settings);
                statuses.Clear();
                foreach (var host in settings.Hosts) statuses.Add(new HostStatusRow { Hostname = host.Hostname, Message = "Waiting for check" });
                service.HostStatus += (host, message) => Dispatcher.InvokeAsync(() =>
                {
                    var row = statuses.FirstOrDefault(r => r.Hostname == host);
                    if (row != null) row.Message = DateTime.Now.ToString("HH:mm:ss") + " · " + message;
                });
                service.Addresses += (v4, v6) => Dispatcher.InvokeAsync(() => { IPv4Status.Text = "IPv4: " + v4; IPv6Status.Text = "IPv6: " + v6; });
                cancellation = new CancellationTokenSource();
                return true;
            }
            catch (ArgumentException ex) { RunStatus.Text = ex.Message; return false; }
        }
        private void Start()
        {
            if (monitoring != null && !monitoring.IsCompleted || !EnsureService()) return;
            StartButton.IsEnabled = false; StopButton.IsEnabled = true;
            monitoring = MonitorAsync(cancellation.Token);
        }
        private async Task MonitorAsync(CancellationToken ct)
        {
            try
            {
                while (true)
                {
                    RunStatus.Text = "Checking addresses…";
                    await service.CheckAsync(ct);
                    RunStatus.Text = "Monitoring · next check at " + DateTime.Now.AddMinutes(5).ToString("HH:mm");
                    await Task.Delay(TimeSpan.FromMinutes(5), ct);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception) { RunStatus.Text = "Monitoring stopped after an unexpected error. Save settings and restart."; StartButton.IsEnabled = true; }
        }
        private async Task StopAsync()
        {
            cancellation?.Cancel();
            if (monitoring != null) await monitoring;
            if (manualCheck != null) await manualCheck;
            service?.Dispose(); service = null;
            cancellation?.Dispose(); cancellation = null; monitoring = null; manualCheck = null;
            RunStatus.Text = "Stopped"; StartButton.IsEnabled = true; StopButton.IsEnabled = false;
        }
        private void Start_Click(object sender, RoutedEventArgs e) => Start();
        private async void Stop_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false; CheckButton.IsEnabled = false; SaveButton.IsEnabled = false; StopButton.IsEnabled = false;
            await StopAsync(); CheckButton.IsEnabled = true; SaveButton.IsEnabled = true;
        }
        private async void Check_Click(object sender, RoutedEventArgs e)
        {
            if (!EnsureService()) return;
            CheckButton.IsEnabled = false;
            manualCheck = CheckOnceAsync(cancellation.Token);
            await manualCheck;
            CheckButton.IsEnabled = true;
        }
        private async Task CheckOnceAsync(CancellationToken ct)
        {
            try { await service.CheckAsync(ct); if (monitoring == null) RunStatus.Text = "Check complete · monitoring is stopped"; }
            catch (OperationCanceledException) { }
            catch (Exception) { RunStatus.Text = "Check failed. Review settings and try again."; }
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var row = new HostEntry(); hosts.Add(row); HostsGrid.SelectedItem = row; HostsGrid.ScrollIntoView(row);
        }
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (HostsGrid.SelectedItem is HostEntry row) hosts.Remove(row);
        }
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            HostsGrid.CommitEdit(DataGridEditingUnit.Cell, true); HostsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            var updated = new Settings
            {
                UpdateUrl = EndpointBox.Text.Trim(), Username = UsernameBox.Text.Trim(), Password = PasswordInput.Password,
                IPv4LookupUrl = IPv4Box.Text.Trim(), IPv6LookupUrl = IPv6Box.Text.Trim(), AutoStart = AutoStartBox.IsChecked == true,
                Hosts = hosts.Select(h => new HostEntry { Hostname = h.Hostname, Mode = h.Mode }).ToList()
            };
            try { updated.Validate(); store.Save(updated); }
            catch (ArgumentException ex) { SaveStatus.Text = ex.Message; return; }
            catch (Exception) { SaveStatus.Text = "Could not save settings. Check access to " + store.FilePath; return; }
            bool wasRunning = monitoring != null && !monitoring.IsCompleted;
            SaveButton.IsEnabled = false; StartButton.IsEnabled = false; CheckButton.IsEnabled = false; StopButton.IsEnabled = false;
            await StopAsync(); settings = updated;
            SaveStatus.Text = "Saved to " + store.FilePath;
            if (wasRunning || settings.AutoStart) Start();
            SaveButton.IsEnabled = true; CheckButton.IsEnabled = true;
        }
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (closing) return;
            e.Cancel = true; IsEnabled = false;
            await StopAsync(); closing = true; Close();
        }
    }
}
