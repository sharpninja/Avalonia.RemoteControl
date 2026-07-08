# Android Transport Spike - 2026-05-25

## Question

Can the current `Avalonia.RemoteControl.Server` ASP.NET Core/Kestrel gRPC host be used directly inside an Avalonia Android app?

## Evidence

Environment checks:

```powershell
Get-Command adb
dotnet workload list
adb version
adb devices -l
```

Results:

- `adb.exe` is available at `C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe`.
- .NET Android workload `36.1.43/10.0.100` is installed.
- One attached physical Android device running Android 15 was available.

Transport build check:

```powershell
$tmp = Join-Path $env:TEMP "avalonia-xplat-<id>"
dotnet new avalonia.xplat -o $tmp -n AdbSpikeProbe -f net10.0 -av 12.0.3 --no-update-check
dotnet add "$tmp\AdbSpikeProbe.Android\AdbSpikeProbe.Android.csproj" reference "F:\GitHub\Avalonia.RemoteControl\src\Avalonia.RemoteControl.Server\Avalonia.RemoteControl.Server.csproj"
dotnet build "$tmp\AdbSpikeProbe.Android\AdbSpikeProbe.Android.csproj" -c Debug
```

Observed failure:

```text
NETSDK1082: There was no runtime pack for Microsoft.AspNetCore.App available for the specified RuntimeIdentifier 'android-arm64'.
```

## Decision

The current ASP.NET Core/Kestrel gRPC host is not viable as the Android app-side transport. Android support must use an Android-compatible bridge or adapter while preserving authentication, marker discovery, dispatcher-safe tree capture, and ADB cleanup requirements.

## Alternatives

- Direct ASP.NET Core/Kestrel gRPC in the Android app: rejected by `NETSDK1082`.
- Keep ADB client workflow and implement an Android-compatible bridge behind it: selected for the next slice.
- Defer Android support entirely: rejected because Android ADB connectivity is a first-class product requirement.

## Affected Requirements

- `FR-ADB-002`
- `FR-ADB-004`
- `FR-SEC-004`
- `TR-ADB-CONNECTIVITY-008`
- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-010`
- `TEST-ADB-006`
- `TEST-ADB-007`
- `TEST-MANUAL-003`
