using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web;
namespace StratoDomainDDNSChanger.Core
{
    public static class Protocol
    {
        public static Uri ValidateHttps(string value)
        {
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || uri.Scheme != "https" || !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Fragment))
                throw new ArgumentException("Use an HTTPS URL without embedded credentials or a fragment.");
            return uri;
        }
        public static string NormalizeHostname(string value)
        {
            value = (value ?? "").Trim().TrimEnd('.').ToLowerInvariant();
            if (value.Length > 253 || !value.Contains(".") || !value.Split('.').All(label => Regex.IsMatch(label, @"^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$")))
                throw new ArgumentException("Enter a full hostname, such as game.example.com, without a port or https://.");
            IPAddress parsed;
            if (IPAddress.TryParse(value, out parsed)) throw new ArgumentException("Enter a DNS hostname, not an IP address.");
            return value;
        }
        public static string NormalizeAddress(string value, AddressFamily family)
        {
            IPAddress address;
            if (!IPAddress.TryParse((value ?? "").Trim(), out address) || address.AddressFamily != family || IPAddress.IsLoopback(address)
                || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any)
                || (family == AddressFamily.InterNetworkV6 && (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.ScopeId != 0)))
                throw new ArgumentException("The IP lookup did not return a usable address of the requested family.");
            return address.ToString();
        }
        public static string AddressList(HostEntry host, string ipv4, string ipv6)
        {
            switch (host.Mode)
            {
                case AddressMode.IPv4Only: return NormalizeAddress(ipv4, AddressFamily.InterNetwork);
                case AddressMode.IPv6Only: return NormalizeAddress(ipv6, AddressFamily.InterNetworkV6);
                case AddressMode.IPv4AndIPv6: return NormalizeAddress(ipv4, AddressFamily.InterNetwork) + "," + NormalizeAddress(ipv6, AddressFamily.InterNetworkV6);
                default: throw new ArgumentException("Unknown address mode.");
            }
        }
        public static Uri BuildUpdateUri(string endpoint, HostEntry host, string ipv4, string ipv6)
        {
            var builder = new UriBuilder(ValidateHttps(endpoint));
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["hostname"] = NormalizeHostname(host.Hostname);
            query["myip"] = AddressList(host, ipv4, ipv6);
            builder.Query = query.ToString();
            return builder.Uri;
        }
        public static string ResponseCode(string body)
        {
            var lines = (body ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length != 1) return "invalid-response";
            string code = lines[0].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            // Only display known protocol codes, never arbitrary response bodies or request URIs.
            switch (code)
            {
                case "good": case "nochg": case "badauth": case "nohost": case "notfqdn": case "badagent":
                case "!donator": case "abuse": case "911": case "dnserr": case "badsys": return code;
                default: return "invalid-response";
            }
        }
        public static bool Success(string code) => code == "good" || code == "nochg";
        public static bool PermanentFailure(string code) => new[] { "badauth", "nohost", "notfqdn", "badagent", "!donator", "abuse", "badsys" }.Contains(code);
    }
}
