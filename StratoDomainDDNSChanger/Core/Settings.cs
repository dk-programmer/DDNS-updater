using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;
namespace StratoDomainDDNSChanger.Core
{
    public enum AddressMode { IPv4Only, IPv6Only, IPv4AndIPv6 }
    public sealed class HostEntry
    {
        public string Hostname { get; set; } = "";
        public AddressMode Mode { get; set; } = AddressMode.IPv4Only;
    }
    public sealed class Settings
    {
        public int Version { get; set; } = 1;
        public string UpdateUrl { get; set; } = "https://dyndns.strato.com/nic/update";
        public string Username { get; set; } = "";
        [ScriptIgnore] public string Password { get; set; } = "";
        public string ProtectedPassword { get; set; } = "";
        public string IPv4LookupUrl { get; set; } = "https://api.ipify.org";
        public string IPv6LookupUrl { get; set; } = "https://api6.ipify.org";
        public bool AutoStart { get; set; } = true;
        public List<HostEntry> Hosts { get; set; } = new List<HostEntry>();
        public void Validate()
        {
            Protocol.ValidateHttps(UpdateUrl);
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password))
                throw new ArgumentException("Enter the provider username and password.");
            if (Username.Contains(":")) throw new ArgumentException("The username cannot contain a colon.");
            if (Hosts == null || Hosts.Count == 0) throw new ArgumentException("Add at least one hostname.");
            foreach (var host in Hosts)
            {
                host.Hostname = Protocol.NormalizeHostname(host.Hostname);
                if (!Enum.IsDefined(typeof(AddressMode), host.Mode)) throw new ArgumentException("Choose an address mode for each hostname.");
            }
            if (Hosts.Select(h => h.Hostname).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Hosts.Count)
                throw new ArgumentException("Each hostname may appear only once.");
            if (Hosts.Any(h => h.Mode != AddressMode.IPv6Only)) Protocol.ValidateHttps(IPv4LookupUrl);
            if (Hosts.Any(h => h.Mode != AddressMode.IPv4Only)) Protocol.ValidateHttps(IPv6LookupUrl);
        }
    }
}
