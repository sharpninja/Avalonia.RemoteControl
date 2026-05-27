# Android ADB Connections

The client can connect to an Avalonia app running on an Android emulator or connected device through `adb forward`.

## Prerequisites

- Android SDK platform tools on `PATH`.
- A running emulator or connected device visible to `adb devices -l`.
- A debuggable app build.
- An app-side runtime bridge listener using `SharpNinja.Avalonia.RemoteControl.Runtime`.

The desktop server package depends on ASP.NET Core/Kestrel and is not the Android app-side transport.

## List Devices

```powershell
avalonia-remote adb list
```

## Connect With Explicit Port And Token

Use this when you already know the device-side listener port:

```powershell
avalonia-remote adb connect --serial emulator-5554 --device-port 47100 --token <token> --keep-forward
```

The command creates:

```text
adb -s <serial> forward tcp:<hostPort> tcp:<devicePort>
```

The default host port is `47100`. Override it with `--host-port`.

When `--keep-forward` succeeds, the command saves the forwarded endpoint, token, and transport protocol into the desktop client's default connection profile. Launch `avalonia-remote` after the command finishes, verify the saved profile is loaded, and click Connect.

The desktop client can also create this explicit forward from the top Connect button. Select an ADB device, choose `arc-protobuf-v1`, enter the token, and leave Package blank. The client forwards the selected host port to the same device port before probing `http://127.0.0.1:<hostPort>/`.

To open an interactive remote window over ADB, connect first, then click Live View in the desktop client. The saved Android bridge profile uses `arc-protobuf-v1`, and the live window uses that bridge for `WatchTree`, `WatchFrames`, and `SendInput`.

## Connect By Package Marker

Use this when the Android app writes `files/avalonia-remote-control.json` in package-private storage:

```powershell
avalonia-remote adb connect --serial emulator-5554 --package com.example.app --keep-forward
```

The client reads the marker with:

```text
adb -s <serial> shell run-as <package> cat files/avalonia-remote-control.json
```

The marker must include:

- `devicePort`
- `token`
- `bridgeProtocol` set to `arc-protobuf-v1`

The client fails closed before forwarding when the marker advertises an unsupported protocol.

For Android bridge markers, the saved desktop profile uses the `arc-protobuf-v1` transport. This prevents the desktop client from reopening the forwarded endpoint with the default gRPC transport.

## Cleanup

If you used `--keep-forward`, remove the forward later:

```powershell
avalonia-remote adb cleanup --serial emulator-5554 --host-port 47100
```

Without `--keep-forward`, the CLI removes the forward after a successful probe.

## Android App-Side Bridge Shape

An Android bridge host should:

1. Register `AddAvaloniaRemoteControlRuntime`.
2. Provide an `IRemoteControlRootProvider`.
3. Configure `IsEnabled`, `Host = IPAddress.Loopback`, `IsAdbTunnel = true`, `AuthenticationToken`, and `Port`.
4. Start `RemoteControlBridgeTcpListener`.
5. Write `RemoteControlBridgeEndpointMarker` to the Android package files directory.
6. Stop the listener on app shutdown.

The sample `samples/Avalonia.RemoteControl.AndroidProbe.Android` demonstrates this pattern.

Live screenshots require `AllowRemoteFrames = true`. Remote input requires both `AllowRemoteActions = true` and `AllowRemoteInput = true`. Keep those options restricted to debuggable builds because the bridge can expose app visuals and mutate app state through forwarded input.

See [Settings Guide](settings.md) for detailed explanations of `--serial`, `--package`, `--device-port`, `--host-port`, `--token`, `--transport-protocol`, `--keep-forward`, `IsAdbTunnel`, and Android bridge live-view settings.

## Common ADB Limits

- `run-as` works only for debuggable packages.
- Device authorization must be accepted before `adb devices` reports `device`.
- ADB forwarding does not remove bearer authentication.
- Package marker discovery is optional; explicit `--device-port` and `--token` are the fallback.
