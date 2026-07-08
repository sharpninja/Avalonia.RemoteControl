# Avalonia.RemoteControl

Avalonia.RemoteControl is a debugging and remote-control system for Avalonia 12 applications.

Current release: `0.7.4`.

The tool gives developers and QA engineers a desktop control surface for a running Avalonia app: inspect the live control tree, view public state, stream logs, invoke guarded actions, edit approved properties, open a live remote UI panel, connect through ADB, and launch an embedded Codex terminal that can drive the connected app through the tool's in-process MCP server.

## Install The Client

```powershell
dotnet tool install --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.7.4
avalonia-remote
```

Update an existing install with:

```powershell
dotnet tool update --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.7.4
```

Packages:

- `SharpNinja.Avalonia.RemoteControl.Protocol` - shared gRPC and bridge protocol contracts.
- `SharpNinja.Avalonia.RemoteControl.Runtime` - Android-compatible shared runtime SDK.
- `SharpNinja.Avalonia.RemoteControl.Server` - embeddable server SDK for debuggee applications.
- `SharpNinja.Avalonia.RemoteControl.Tool` - .NET tool that launches the remote-control client through `avalonia-remote`.

Running `avalonia-remote` with no arguments opens the desktop client. `avalonia-remote --help` and `avalonia-remote adb ...` run command-line workflows. The desktop client can save and forget endpoint, token, certificate, transport, project, layout, and replay state in user-scoped application data.

## Start Here

- [Getting started](docs/user/getting-started.md) - local desktop quickstart.
- [Android ADB connections](docs/user/android-adb.md) - emulator or connected-device quickstart.
- [Codex MCP](docs/user/codex-mcp.md) - launch Codex inside the tool and drive the connected app through MCP.
- [Client tool](docs/user/client-tool.md) - desktop UI, docking, logs, live view, projects, and CLI workflows.
- [Server integration](docs/user/server-integration.md)
- [Settings guide](docs/user/settings.md)
- [Security guide](docs/user/security.md)
- [Troubleshooting](docs/user/troubleshooting.md)

## Current Capabilities

- Inspect the current Avalonia control tree and selected-node properties.
- Stream tree changes, bounded redacted `ILogger` data, and guarded live screenshot frames.
- Invoke focus, clicks, remote input, and approved public property edits when the debuggee enables those gates.
- Connect over local loopback, TLS network endpoints, or Android ADB forwarding.
- Use the desktop UI to refresh ADB devices, create forwards, connect, and clean up forwards.
- Dock or float tool panels, including live view and logs, with persisted layout state.
- Store user-scoped projects with connection profiles, session log history, interaction journals, replay data, and tree diffs.
- Launch an embedded Codex terminal with `avalonia_remote_control` MCP tools connected to the running desktop tool.

Security posture:

- disabled by default
- authentication required for all sessions
- TLS required for non-loopback listeners
- ADB tunnels remain authenticated
- mutation is deny-by-default
- remote actions are disabled unless explicitly enabled
- frame streaming is disabled unless explicitly enabled
- remote input is disabled unless remote actions and remote input are explicitly enabled
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
