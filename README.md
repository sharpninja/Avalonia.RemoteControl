# Avalonia.RemoteControl

Avalonia.RemoteControl is a debugging and remote-control system for Avalonia 12 applications.

The project is in early Byrd implementation. Current deliverables include requirements, architecture, solution skeleton, package metadata, CI scaffolding, gRPC protocol contracts, hosted gRPC startup, Android-compatible runtime/bridge foundations, bearer-token RPC authentication, read-only tree snapshots, live tree streaming, guarded property mutation, guarded click invocation, bounded `ILogger` streaming, a basic Avalonia desktop client UI, and ADB list/connect/cleanup CLI workflows.

Packages:

- `Avalonia.RemoteControl.Server` - embeddable server SDK for debuggee applications.
- `Avalonia.RemoteControl.Tool` - .NET tool that launches the remote-control client through `avalonia-remote`.

Running `avalonia-remote` with no arguments opens the desktop client. `avalonia-remote --help` and `avalonia-remote adb ...` run command-line workflows. The desktop client can save and forget its default endpoint/token profile in user-scoped application data.

Security posture:

- disabled by default
- authentication required for all sessions
- TLS required for non-loopback listeners
- ADB tunnels remain authenticated
- mutation is deny-by-default
- remote actions are disabled unless explicitly enabled
- sensitive properties and log fields are redacted

Build and validation:

```powershell
dotnet restore Avalonia.RemoteControl.slnx
dotnet build Avalonia.RemoteControl.slnx --configuration Release --no-restore
dotnet test Avalonia.RemoteControl.slnx --configuration Release --no-build
dotnet pack Avalonia.RemoteControl.slnx --configuration Release --no-build --output artifacts/packages
```

ADB CLI:

```powershell
avalonia-remote adb list
avalonia-remote adb connect --serial emulator-5554 --device-port 47100 --token <token> --keep-forward
avalonia-remote adb connect --serial emulator-5554 --package com.example.app --keep-forward
avalonia-remote adb cleanup --serial emulator-5554 --host-port 47100
```
