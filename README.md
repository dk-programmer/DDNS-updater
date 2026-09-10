# DDNS Updater 0.6

A Windows desktop app for Dynamic DNS, with separate IPv4/IPv6 settings for every domain and subdomain. Built for Strato's DynDNS v2 endpoint. Other providers need compatible `hostname` / `myip` parameters and `good` / `nochg` responses; provider-specific extensions are not guaranteed.

## Set up

1. Enable DynDNS for each domain or subdomain in your provider's control panel.
2. Run `StratoDomainDDNSChanger.exe`, open **Settings**, and enter the update URL, username and password.
3. Use **Add hostname** for each full hostname, for example `example.com` and `game.example.com`.
4. Choose **IPv4Only**, **IPv6Only**, or **IPv4AndIPv6** independently for each row.
5. Save. Use **Start** to monitor or **Check now** for one check.

A shared `game.example.com` hostname can serve all games on one public IPv4 address; each game uses its own port. Keep the website hostname on IPv4AndIPv6 when appropriate. IPv4Only omits IPv6 from updates; it does **not** remove an existing AAAA record at the provider. A newly created DynDNS hostname that receives only IPv4 has been verified on Strato without AAAA.

The default lookup services are `https://api.ipify.org` (IPv4) and `https://api6.ipify.org` (IPv6). They can be changed under **IP lookup services**. An IPv6 lookup must return the address of the computer hosting your service; check that it matches the address permitted by your router, especially on systems with temporary IPv6 addresses.

## Monitoring

- Checks every five minutes; one accepted address set per hostname is remembered for the current run.
- Publishes once on startup and whenever that hostname's selected address set changes.
- A failed update is retried even if the detected IP is unchanged. A failed IPv6 lookup does not prevent an IPv4-only hostname from updating.
- Known authentication/configuration errors pause that hostname until settings are saved or the service is restarted. Provider `911` and HTTP 429 responses trigger a 30-minute pause.
- Switching tabs does not stop monitoring. Stop cancels active requests; closing the app stops monitoring.
- HTTPS certificate validation remains enabled. Redirects are not followed. Credentials are sent only to the update endpoint, never to the IP lookup services.
- **Start monitoring when this app opens** controls polling, not Windows startup. To launch on Windows sign-in, create a shortcut to the EXE in the Startup folder (`shell:startup`). Only one app instance is allowed per user session.

## Settings and credentials

Settings are stored at `%LOCALAPPDATA%\DDNS Updater\config.json`, outside the executable folder and Git repository. The password is encrypted with Windows DPAPI for the current user. Run the app as the same Windows user that saved it. Do not copy this config to another user or machine expecting the password to decrypt.

No live config or credential files belong in the repository or release ZIP. The `.gitignore` excludes configs, IDE state, build output and logs. Test configs use fake credentials and stay under ignored `artifacts/`.

For an upgrade from 0.5, existing settings can be re-entered in the UI. The test utility also provides an explicit migration command for a main domain plus an IPv4-only subdomain:

```powershell
.\tests\bin\Release\DdnsUpdater.Tests.exe --migrate C:\path\to\old\config.json example.com game.example.com
```

Run migration as the user who will run the app. It refuses to overwrite an existing destination config and validates that the encrypted password can be read back. After successful migration, move old plaintext configs and backups outside all Git checkouts. The new app never reads a config beside its executable.

## Build and test

Requires Windows, .NET Framework 4.7.2 and Visual Studio / Build Tools with .NET desktop development and its targeting pack. No NuGet packages are required.

```powershell
.\build.ps1
```

This builds the app, runs the regression suite, and writes `artifacts/DDNS-Updater-0.6.0.zip` with the EXE, runtime config, README and license. It never packages saved settings. `build-release.bat` is a wrapper for the same command.

Tests cover per-host modes, IP changes, independent failures, retries, authentication isolation, provider backoff, cancellation, config encryption/round trips, and loading/rendering the WPF views. Under a sandbox account without a loaded Windows profile, `build.ps1 -SkipProtectionTests` skips DPAPI tests explicitly; run the full tests in a normal user session before release.

An optional live check uses the current user's saved config, performs actual provider updates, and fails unless all hostnames are accepted:

```powershell
.\tests\bin\Release\DdnsUpdater.Tests.exe --live-check
```

## Changes from 0.5

Replaced the single comma-separated domain field with a hostname table and per-host address modes. Removed unused NuGet packages, custom animation/converter layers, IDE caches, and app-local credential storage. Simplified to one cancellable monitor, preserved IP lookup configuration, and separated detected addresses from successful publishes.

Licensed under [MIT](LICENSE). By [dk-programmer](https://github.com/dk-programmer).
