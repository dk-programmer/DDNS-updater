# DDNS Updater

A small Windows desktop application that keeps your Dynamic DNS records in sync with your current public IP addresses. It polls for IPv4 and IPv6 changes on a fixed interval and pushes updates to any provider that speaks the common **DynDNS update protocol** (HTTP GET with `hostname` / `myip`, plus HTTP Basic authentication).

Originally built for [Strato](https://www.strato.de/) domains, but the update URL and credentials are fully configurable, so it also works with services such as **No-IP**, **DynDNS**, and other DynDNS-compatible hosts.

## Features

- **IPv4 and IPv6** — Detects both address families via [ipify](https://www.ipify.org/) and sends them to your provider when they change.
- **Provider-agnostic** — Set your own update endpoint URL, hostname, username, and password; no provider is hard-coded except a Strato default in the UI.
- **Update only on change** — DDNS is called when a public IP changes, not on every poll cycle.
- **Configurable polling** — Checks public IPs every **5 minutes** (fixed in the current release).
- **Autorun** — Optionally start monitoring automatically when the app launches.
- **Ignore errors** — Optionally suppress error dialogs when an update request fails (useful for unattended runs).
- **Live status** — Home view shows domain, current IPv4/IPv6, last update result, countdown to the next check, and activity indicators while fetching IPs or calling DDNS.
- **Persistent settings** — Configuration is stored in `config.json` next to the executable (JSON, indented).
- **Lightweight WPF UI** — Borderless window with drag-to-move, minimize, and close controls.

## Requirements

- **Windows**
- **.NET Framework 4.7.2** or later
- **Visual Studio 2017+** (or Build Tools) to compile from source
- Network access to your DDNS provider and to the public IP lookup services (`api.ipify.org`, `api6.ipify.org`)

## Supported providers

The app uses the standard DynDNS-style update request:

```http
GET {UpdateUrl}?hostname={domain}&myip={ipv4},{ipv6}
Authorization: Basic {username}:{password}
```

That pattern is supported by many services, including:

| Provider | Example update URL |
|----------|-------------------|
| Strato | `https://dyndns.strato.com/nic/update` |
| No-IP | `https://dynupdate.no-ip.com/nic/update` |
| DynDNS / compatible hosts | Your provider’s documented `nic/update` URL |

Providers that use a **different** API shape (for example token-only URLs like DuckDNS) may not work without adapting the update URL or credentials to match what your host expects. If you use DuckDNS or similar, check their docs and map fields accordingly (hostname, token/password, custom query string).

## Getting started

### Download

Check [Releases](https://github.com/dk-programmer/ddns-updater/releases) for pre-built binaries (e.g. tag `v0.5.1`).

### Build from source

1. Clone the repository:

   ```bash
   git clone https://github.com/dk-programmer/ddns-updater.git
   cd ddns-updater
   ```

2. Open `StratoDomainDDNSChanger/StratoDomainDDNSChanger.csproj` in Visual Studio.

3. Restore NuGet packages if prompted.

4. Build **Release** (`StratoDomainDDNSChanger.exe` is written to `StratoDomainDDNSChanger/bin/Release/`).

## Configuration

1. Run the application.
2. Open **Config** in the sidebar.
3. Set:
   - **Domain Url** — Hostname registered with your DDNS provider.
   - **Username** / **Password** — Provider credentials (HTTP Basic auth).
   - **Update Url** — Provider update endpoint (default: Strato `https://dyndns.strato.com/nic/update`).
   - **Autorun** — Start IP monitoring when the app starts.
   - **Ignore Error** — Do not show message boxes on failed DDNS updates.
4. Click **Save**. Settings are written to `config.json` in the application directory.

### Example `config.json`

```json
{
  "WebsiteUrl": "your-domain.example.com",
  "UserName": "your-username",
  "Password": "your-password",
  "UpdateUrl": "https://dyndns.strato.com/nic/update",
  "LastIPv4": "",
  "LastIPv6": "",
  "LastUpdated": "",
  "GetSelfIPv4Url": "https://api.ipify.org",
  "GetSelfIPv6Url": "",
  "Autorun": "true",
  "IgnoreError": "true",
  "NextUpdate": "0"
}
```

> **Note:** `GetSelfIPv4Url` / `GetSelfIPv6Url` are stored in config but the current build always uses ipify for public IP detection.

## Usage

1. Configure your provider settings and save.
2. On the **Home** screen, click **Start Process** to begin polling (or enable **Autorun** and restart the app).
3. The app will:
   - Fetch your public IPv4 and IPv6 addresses.
   - Compare them to the last known values.
   - Call your update URL when an address changes.
   - Show **Last Updated** and a **Next Update** countdown between polls.

You can minimize the window and leave it running in the background; switching away from the Home view stops active polling tasks.

## Project layout

```
ddns-updater/
└── StratoDomainDDNSChanger/   # WPF application (.NET Framework 4.7.2)
    ├── Core/                  # Config, DDNS logic, IP polling
    ├── MVVM/                  # Views and view models
    └── Theme/                 # XAML styles
```

The solution name `StratoDomainDDNSChanger` is historical; the window title and repo name are **DDNS Updater**.

## License

This project is licensed under the [MIT License](LICENSE).

## Author

[dk-programmer](https://github.com/dk-programmer)
