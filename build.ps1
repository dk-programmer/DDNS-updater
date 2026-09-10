param([switch]$SkipProtectionTests)
$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path -LiteralPath $vswhere)) { throw 'Install Visual Studio Build Tools with .NET desktop development and the .NET Framework 4.7.2 targeting pack.' }
$msbuild = & $vswhere -latest -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'MSBuild not found.' }
& $msbuild (Join-Path $PSScriptRoot 'tests\Tests.csproj') /t:Build /p:Configuration=Release /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
$testArguments = @((Join-Path $PSScriptRoot 'artifacts\tests'))
if ($SkipProtectionTests) { $testArguments += '--skip-protection' }
& (Join-Path $PSScriptRoot 'tests\bin\Release\DdnsUpdater.Tests.exe') @testArguments
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
$output = Join-Path $PSScriptRoot 'StratoDomainDDNSChanger\bin\Release'
$package = Join-Path $PSScriptRoot 'artifacts\DDNS-Updater-0.6.0.zip'
Compress-Archive -LiteralPath (Join-Path $output 'StratoDomainDDNSChanger.exe'),(Join-Path $output 'StratoDomainDDNSChanger.exe.config'),(Join-Path $PSScriptRoot 'README.md'),(Join-Path $PSScriptRoot 'LICENSE') -DestinationPath $package -Force
Write-Output "Built and tested: $package"
