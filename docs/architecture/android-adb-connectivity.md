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

## Planned CLI

```powershell
avalonia-remote adb list
avalonia-remote adb connect --serial emulator-5554 --package com.example.app
avalonia-remote adb connect --serial emulator-5554 --device-port 47100 --token <token>
avalonia-remote adb cleanup --serial emulator-5554
```

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

## Open Design Questions

- Where should Android endpoint metadata live: app log marker, package-accessible file, Android intent, or explicit CLI-only flags?
- How should generated debug tokens be surfaced to the developer without logging secrets?
- Should ADB cleanup remove only forwards created by this client instance or all forwards for the configured port?

## External References

- Android Debug Bridge documentation: https://developer.android.com/tools/adb
- Android reverse port forwarding example documentation: https://developer.android.com/develop/ui/views/layout/webapps/access-local-server
