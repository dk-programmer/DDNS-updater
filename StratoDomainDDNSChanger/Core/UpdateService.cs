using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace StratoDomainDDNSChanger.Core
{
    public sealed class UpdateService : IDisposable
    {
        private readonly Settings settings;
        private readonly HttpClient client;
        private readonly Dictionary<string, string> published = new Dictionary<string, string>();
        private readonly HashSet<string> blocked = new HashSet<string>();
        private readonly SemaphoreSlim gate = new SemaphoreSlim(1, 1);
        private DateTime providerRetryAfter = DateTime.MinValue;
        public event Action<string, string> HostStatus;
        public event Action<string, string> Addresses;
        public UpdateService(Settings settings, HttpMessageHandler handler = null)
        {
            settings.Validate();
            this.settings = settings;
            client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false });
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("DDNS-Updater/0.6.0");
        }
        private void Report(HostEntry host, string message) => HostStatus?.Invoke(host.Hostname, message);
        private async Task<string> Lookup(string url, AddressFamily family, CancellationToken ct)
        {
            try
            {
                using (var response = await client.GetAsync(Protocol.ValidateHttps(url), ct))
                {
                    response.EnsureSuccessStatusCode();
                    return Protocol.NormalizeAddress(await response.Content.ReadAsStringAsync(), family);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return null; }
            catch (HttpRequestException) { return null; }
            catch (ArgumentException) { return null; }
        }
        public async Task CheckAsync(CancellationToken ct)
        {
            await gate.WaitAsync(ct);
            try
            {
                var v4 = settings.Hosts.Any(h => h.Mode != AddressMode.IPv6Only) ? await Lookup(settings.IPv4LookupUrl, AddressFamily.InterNetwork, ct) : null;
                var v6 = settings.Hosts.Any(h => h.Mode != AddressMode.IPv4Only) ? await Lookup(settings.IPv6LookupUrl, AddressFamily.InterNetworkV6, ct) : null;
                Addresses?.Invoke(v4 ?? "Unavailable", v6 ?? "Unavailable / not needed");
                foreach (var host in settings.Hosts)
                {
                    ct.ThrowIfCancellationRequested();
                    if (blocked.Contains(host.Hostname)) { Report(host, "Provider rejected settings. Save corrected settings to retry."); continue; }
                    if ((host.Mode != AddressMode.IPv6Only && v4 == null) || (host.Mode != AddressMode.IPv4Only && v6 == null))
                    { Report(host, "IP lookup failed; retry at next check."); continue; }
                    string addresses = Protocol.AddressList(host, v4, v6);
                    string old;
                    if (published.TryGetValue(host.Hostname, out old) && old == addresses)
                    { Report(host, "Up to date: " + addresses); continue; }
                    if (DateTime.UtcNow < providerRetryAfter) { Report(host, "Provider requested a pause; will retry later."); continue; }
                    try
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Get, Protocol.BuildUpdateUri(settings.UpdateUrl, host, v4, v6)))
                        {
                            // Attach credentials only to provider updates, never to IP lookup services.
                            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(settings.Username + ":" + settings.Password)));
                            using (var response = await client.SendAsync(request, ct))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden) blocked.Add(host.Hostname);
                                    if ((int)response.StatusCode == 429) providerRetryAfter = DateTime.UtcNow.AddMinutes(30);
                                    Report(host, "HTTP " + (int)response.StatusCode + (blocked.Contains(host.Hostname) ? "; check credentials and save." : "; will retry."));
                                    continue;
                                }
                                string code = Protocol.ResponseCode(await response.Content.ReadAsStringAsync());
                                if (Protocol.Success(code))
                                {
                                    published[host.Hostname] = addresses;
                                    Report(host, "Accepted (" + code + "): " + addresses);
                                }
                                else
                                {
                                    if (Protocol.PermanentFailure(code)) blocked.Add(host.Hostname);
                                    if (code == "911") providerRetryAfter = DateTime.UtcNow.AddMinutes(30);
                                    Report(host, code + (blocked.Contains(host.Hostname) ? "; check settings and save." : "; will retry."));
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) when (!ct.IsCancellationRequested) { Report(host, "Update timed out; will retry."); }
                    catch (HttpRequestException) { Report(host, "Network error; will retry."); }
                }
            }
            finally { gate.Release(); }
        }
        public void Dispose() { client.Dispose(); gate.Dispose(); }
    }
}
