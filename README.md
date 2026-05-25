# Avalonia.RemoteControl

Avalonia.RemoteControl is a debugging and remote-control system for Avalonia 12 applications.

The project is in early Byrd implementation. Current deliverables include requirements, architecture, solution skeleton, package metadata, CI scaffolding, gRPC protocol contracts, read-only tree snapshots, live tree streaming, guarded property mutation, guarded click invocation, and bounded `ILogger` streaming.

Planned packages:

- `Avalonia.RemoteControl.Server` - embeddable server SDK for debuggee applications.
- `Avalonia.RemoteControl.Tool` - .NET tool that launches the remote-control client through `avalonia-remote`.

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
