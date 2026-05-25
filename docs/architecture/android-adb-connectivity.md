# Android ADB Connectivity

## Goal

The client must connect easily to an Avalonia app running in an Android emulator or a device connected through ADB.

The user should not need to manually type `adb forward` commands for normal use.

## User Flow

1. User opens `avalonia-remote`.
2. User selects the `ADB` connection mode.
3. Client lists available ADB devices/emulators.
4. User selects a serial.
5. Client discovers or accepts the target package/port/token configuration.
6. Client creates an ADB forward from a host localhost port to the device app port.
7. Client connects to `127.0.0.1:<hostPort>`.
8. Client cleans up the forward on disconnect by default.

## CLI

```powershell
avalonia-remote adb list
avalonia-remote adb connect --serial emulator-5554 --package com.example.app --keep-forward
avalonia-remote adb connect --serial emulator-5554 --device-port 47100 --token <token> --keep-forward
avalonia-remote adb cleanup --serial emulator-5554 --host-port 47100
```

Default `adb connect` behavior creates the forward, probes the authenticated gRPC endpoint, and removes the forward before exit. Use `--keep-forward` when a follow-on tool or manual diagnostic session should keep the tunnel open.

## Implemented Client Behavior

- `adb list` runs `adb devices -l` and parses serial, state, model, product, and device metadata.
- `adb connect` creates `adb -s <serial> forward tcp:<hostPort> tcp:<devicePort>`.
- `adb connect` requires `--token` unless package marker discovery supplies one.
- `adb connect --package <package>` reads `files/avalonia-remote-control.json` through `adb shell run-as <package> cat ...`.
- After forwarding, the client probes `GetCapabilities` over `http://127.0.0.1:<hostPort>` with bearer authentication.
- `adb cleanup` removes a host forward using `adb -s <serial> forward --remove tcp:<hostPort>`.

## Android Marker

The package marker is a JSON file in app-private storage:

```json
{
  "schemaVersion": "1",
  "devicePort": 47100,
  "token": "debug-session-token",
  "protocol": "grpc"
}
```

The file is intentionally read through `run-as`, so it only works for debuggable packages and does not require broad device storage access. Missing protocol metadata is treated as legacy `grpc`. Android bridge markers must set `bridgeProtocol` or `protocol` to `arc-protobuf-v1`; clients that do not implement that bridge must reject the marker before opening an ADB forward.

## Requirements

Functional requirements:

- `FR-ADB-001`
- `FR-ADB-002`
- `FR-ADB-003`
- `FR-ADB-004`
- `FR-ADB-005`
- `FR-ADB-006`

Technical requirements:

- `TR-ADB-CONNECTIVITY-001`
- `TR-ADB-CONNECTIVITY-002`
- `TR-ADB-CONNECTIVITY-003`
- `TR-ADB-CONNECTIVITY-004`
- `TR-ADB-CONNECTIVITY-005`
- `TR-ADB-CONNECTIVITY-006`
- `TR-ADB-CONNECTIVITY-007`
- `TR-ADB-CONNECTIVITY-008`
- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-010`
- `TR-ADB-CONNECTIVITY-011`
- `TR-ADB-CONNECTIVITY-012`
- `TR-ADB-CONNECTIVITY-013`
- `TR-ADB-CONNECTIVITY-014`

Security requirements:

- `FR-SEC-004`
- `TR-SEC-SECURITY-002`
- `TR-SEC-SECURITY-005`
- `TR-SEC-SECURITY-015`

## Technical Spike 0

Before the product depends on Android hosting behavior, prove:

- an Avalonia Android app can expose the selected app-side transport
- the host client can connect through ADB forwarding
- tree capture can run safely through the Avalonia dispatcher on Android
- auth still works over the ADB tunnel
- cleanup removes created forwarding rules

Spike output:

- decision record naming the selected Android app-side transport
- minimal proof commands
- pass/fail evidence
- required changes to protocol/hosting requirements

Current status: host-side ADB workflow is implemented and unit-tested. `adb` is available on the workstation, a physical Android device was detected, and `avalonia-remote adb list` successfully listed it. Marker discovery was tested against an installed non-debuggable Android package and failed with Android's expected `run-as: package not debuggable` protection.

Technical Spike 0 found that the current Kestrel/AspNetCore gRPC server transport is not viable as the Android app-side transport. A throwaway `net10.0-android` app referencing `Avalonia.RemoteControl.Server` restored and compiled project references, then failed Android packaging with `NETSDK1082` because `Microsoft.AspNetCore.App` has no `android-arm64` runtime pack. Android support therefore needs an Android-compatible app-side bridge or transport behind the same desktop-facing protocol instead of directly hosting the current AspNetCore server in-process.

The `arc-protobuf-v1` marker value and bridge envelope contract are now defined in the protocol package. The Android app-side listener, runtime split, and Android probe sample remain open.

Minimal proof commands used:

```powershell
adb version
adb devices -l
dotnet run --project src\Avalonia.RemoteControl.Tool\Avalonia.RemoteControl.Tool.csproj --configuration Release --no-restore -- adb list
$tmp = Join-Path $env:TEMP "avalonia-xplat-<id>"
dotnet new avalonia.xplat -o $tmp -n AdbSpikeProbe -f net10.0 -av 12.0.3 --no-update-check
dotnet add "$tmp\AdbSpikeProbe.Android\AdbSpikeProbe.Android.csproj" reference "F:\GitHub\Avalonia.RemoteControl\src\Avalonia.RemoteControl.Server\Avalonia.RemoteControl.Server.csproj"
dotnet build "$tmp\AdbSpikeProbe.Android\AdbSpikeProbe.Android.csproj" -c Debug
adb -s <serial> shell run-as <debuggable-package> cat files/avalonia-remote-control.json
```

Spike decision: keep the host-side ADB workflow, package-private marker model, bearer authentication, and cleanup behavior. Replace the Android app-side transport plan with an Android-compatible bridge/transport that does not depend on `Microsoft.AspNetCore.App`.

Bridge proof commands once the bridge sample exists:

```powershell
$serial = "<serial>"
$package = "com.sharpninja.avalonia.remotecontrol.androidprobe"
dotnet build .\samples\Avalonia.RemoteControl.AndroidProbe.Android\Avalonia.RemoteControl.AndroidProbe.Android.csproj -c Debug -f net10.0-android
dotnet build .\samples\Avalonia.RemoteControl.AndroidProbe.Android\Avalonia.RemoteControl.AndroidProbe.Android.csproj -c Debug -f net10.0-android -t:Install -p:AndroidDeviceSerial=$serial
adb -s $serial shell cmd package resolve-activity --brief $package
adb -s $serial shell am start -n $package/<resolved-activity>
adb -s $serial shell run-as $package cat files/avalonia-remote-control.json
dotnet run --project .\src\Avalonia.RemoteControl.Tool\Avalonia.RemoteControl.Tool.csproj -- adb connect --serial $serial --package $package --keep-forward
dotnet run --project .\src\Avalonia.RemoteControl.Tool\Avalonia.RemoteControl.Tool.csproj -- adb cleanup --serial $serial --host-port 47100
```

## Open Design Questions

- The default Android endpoint metadata location is the debuggable package-private marker file `files/avalonia-remote-control.json`; explicit CLI flags remain the fallback path.
- Which Android-compatible bridge protocol should back the ADB tunnel while preserving the client capability contract?
- What cancellation and backpressure rules should `arc-protobuf-v1` enforce for streamed tree/log updates?
- How should generated debug tokens be surfaced to the developer without logging secrets?
- Should ADB cleanup remove only forwards created by this client instance or all forwards for the configured port?

## External References

- Android Debug Bridge documentation: https://developer.android.com/tools/adb
- Android reverse port forwarding example documentation: https://developer.android.com/develop/ui/views/layout/webapps/access-local-server
