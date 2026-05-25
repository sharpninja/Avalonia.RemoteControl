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
  "devicePort": 47100,
  "token": "debug-session-token"
}
```

The file is intentionally read through `run-as`, so it only works for debuggable packages and does not require broad device storage access.

## Requirements

Functional requirements:

- `FR-ADB-001`
- `FR-ADB-002`
- `FR-ADB-003`
- `FR-ADB-004`
- `FR-ADB-005`
- `FR-ADB-006`

Technical requirements:

- `TR-ADB-001`
- `TR-ADB-002`
- `TR-ADB-003`
- `TR-ADB-004`
- `TR-ADB-005`
- `TR-ADB-006`
- `TR-ADB-007`
- `TR-ADB-008`

Security requirements:

- `FR-SEC-004`
- `TR-SEC-002`
- `TR-SEC-005`
- `TR-SEC-015`

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

Current status: host-side ADB workflow is implemented and unit-tested. Technical Spike 0 still needs a real emulator/device proof that an Avalonia Android app can host or bridge the selected app-side transport and publish the marker safely.

## Open Design Questions

- Where should Android endpoint metadata live: app log marker, package-accessible file, Android intent, or explicit CLI-only flags?
- How should generated debug tokens be surfaced to the developer without logging secrets?
- Should ADB cleanup remove only forwards created by this client instance or all forwards for the configured port?

## External References

- Android Debug Bridge documentation: https://developer.android.com/tools/adb
- Android reverse port forwarding example documentation: https://developer.android.com/develop/ui/views/layout/webapps/access-local-server
