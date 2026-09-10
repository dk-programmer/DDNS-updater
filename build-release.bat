@echo off
setlocal EnableExtensions
cd /d "%~dp0"

set "LOG=%~dp0build-release.log"
echo ===== DDNS Updater build %DATE% %TIME% ===== > "%LOG%"
echo Working directory: %CD%>> "%LOG%"
echo.

set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
set "MSBUILD="

if exist "%VSWHERE%" (
  echo Looking for MSBuild via vswhere...
  echo Looking for MSBuild via vswhere...>> "%LOG%"
  for /f "usebackq delims=" %%i in (`"%VSWHERE%" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"`) do set "MSBUILD=%%i"
) else (
  echo vswhere.exe not found at:
  echo   %VSWHERE%
  echo vswhere.exe not found>> "%LOG%"
)

if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles%\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe" set "MSBUILD=%ProgramFiles(x86)%\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
if not defined MSBUILD if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" set "MSBUILD=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"

if not defined MSBUILD (
  echo.
  echo ERROR: MSBuild was not found.
  echo Install "Visual Studio Build Tools" with the MSBuild / .NET desktop build tools workload.
  echo.
  echo ERROR: MSBuild was not found.>> "%LOG%"
  goto :END
)

echo Using MSBuild:
echo   %MSBUILD%
echo Using MSBuild: %MSBUILD%>> "%LOG%"
echo.

set "CSPROJ=%~dp0StratoDomainDDNSChanger\StratoDomainDDNSChanger.csproj"
if not exist "%CSPROJ%" (
  echo ERROR: Project not found:
  echo   %CSPROJ%
  echo ERROR: Project not found: %CSPROJ%>> "%LOG%"
  goto :END
)

echo Building Release...
echo Building Release...>> "%LOG%"
"%MSBUILD%" "%CSPROJ%" /t:Restore,Build /p:Configuration=Release /p:Platform=AnyCPU /v:m >> "%LOG%" 2>&1
set "BUILD_EXIT=%ERRORLEVEL%"
type "%LOG%"
echo.
if not "%BUILD_EXIT%"=="0" (
  echo BUILD FAILED with exit code %BUILD_EXIT%
  echo See log: %LOG%
  goto :END
)

set "OUT=%~dp0StratoDomainDDNSChanger\bin\Release"
echo Build succeeded.
echo Output folder: %OUT%
dir /b "%OUT%\StratoDomainDDNSChanger.exe" 2>nul
if errorlevel 1 (
  echo WARNING: EXE not found in Release folder. Check Platform target / output path in the log.
)

set "DEST=%USERPROFILE%\Desktop\DDNS Updater 0.5.0"
if exist "%DEST%\" (
  echo.
  echo Copying build output to:
  echo   %DEST%
  xcopy /y /q "%OUT%\*" "%DEST%\" >> "%LOG%" 2>&1
  if errorlevel 1 (
    echo COPY FAILED - close DDNS Updater if it is running, then run this script again.
  ) else (
    echo Copy done.
  )
) else (
  echo Desktop folder not found, skipped copy: %DEST%
)

echo.
echo Log saved to: %LOG%

:END
echo.
echo Press any key to close...
pause >nul
endlocal
