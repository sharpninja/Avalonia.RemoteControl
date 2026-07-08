# Architecture Overview

## System Shape

Avalonia.RemoteControl has four major parts:

- Server SDK embedded into the debuggee app.
- Versioned gRPC protocol contracts.
- Desktop client UI launched through a .NET tool.
- Android/ADB connection support for emulator and attached-device apps.

## Server SDK

The server SDK will be packaged as `SharpNinja.Avalonia.RemoteControl.Server`.

Implemented responsibilities:

- register services through `IServiceCollection`
- start the transport through `IServiceProvider`
- capture Avalonia tree snapshots through the UI dispatcher
- expose safe property metadata and mutations
- invoke supported actions
- capture `ILogger` output through a bounded provider
- enforce disabled-by-default startup state, bearer authentication, listener/TLS policy validation, redaction, and deny-by-default mutation/action gates
- integrate server start/stop with Avalonia controlled application lifetime events
- emit sanitized diagnostic/audit log records for remote-control events, command results, and security decisions

## Protocol

The desktop-facing protocol is versioned gRPC.

Defined RPCs:

- `GetCapabilities`
- `GetSnapshot`
- `WatchTree`
- `InvokeClick`
- `SetProperty`
- `WatchLogs`
- `WatchFrames`
- `SendInput`

The protocol must support version/capability negotiation because Android transport feasibility may force different app-side transport internals while preserving the desktop-facing contract.

## Client

The client is an Avalonia desktop application launched by the .NET tool package `SharpNinja.Avalonia.RemoteControl.Tool`.

Command:

```powershell
avalonia-remote
```

Implemented client areas:

- endpoint/token connection controls
- Android ADB device discovery, package marker discovery, forwarding, connection, and cleanup controls
- visual/control tree view
- selected-node property list
- property mutation command
- click command
- log stream viewer
- docked and floating live view with screenshot and tree replica render modes
- pointer, wheel, keyboard, and text input from the live view when enabled
- user-scoped projects, session log history, interaction journals, replay artifacts, and layout persistence
- embedded terminal with in-process Codex MCP host integration
- connection/status line with protocol, transport, and authenticated audit identity
- user-scoped default connection profile save/forget for endpoint, token, and certificate path
- TLS connection trust through a configured server certificate file
- manual TLS certificate inspection and acceptance by SHA-256 fingerprint

## Android

Android connectivity is a first-class product requirement. The client should automate ADB device selection, forwarding, package/endpoint discovery, connection, and cleanup.

Technical Spike 0 found that the current Kestrel/AspNetCore gRPC server transport cannot be used directly inside a `net10.0-android` app because `Microsoft.AspNetCore.App` has no Android runtime pack. Android support therefore needs an Android-compatible app-side bridge or transport behind the same desktop-facing protocol.

The package-private Android marker is the negotiation point for this split. Missing protocol metadata means the existing gRPC ADB path. Bridge markers must declare `arc-protobuf-v1`; current clients support that marker for authenticated bridge operations and reject unknown protocols before forwarding.

## Current Implementation Status

- `SharpNinja.Avalonia.RemoteControl.Protocol` defines the versioned gRPC and bridge contracts.
- `SharpNinja.Avalonia.RemoteControl.Runtime` provides host-independent runtime services for dispatcher-safe tree snapshots, mutation/action services, logging, bearer authentication, bridge dispatch, and Android-compatible builds.
- `SharpNinja.Avalonia.RemoteControl.Server` starts a Kestrel HTTP/2 gRPC endpoint, enforces bearer authentication, validates listener/TLS startup policy, and hosts the runtime services for desktop/server-capable targets.
- `SharpNinja.Avalonia.RemoteControl.Tool` opens the desktop client UI by default and also provides ADB device listing, forwarding, package marker discovery, authenticated endpoint probing, manual TLS certificate acceptance, and cleanup commands.
- CI files exist for GitHub Actions and Azure Pipelines.
- Broader Android emulator/device matrix coverage remains a future validation slice.

## Security

The remote-control surface is disabled by default and must be explicitly enabled by the debuggee app.

Security controls include:

- authentication on every RPC
- TLS for non-loopback network listeners
- cleartext only for explicit loopback/ADB tunnel cases
- deny-by-default mutation policy
- redaction for sensitive property/log names
- sanitized errors
- audit logs for remote mutations and security failures

See [Security Model](Security-Model).

## Release Shape

Packages:

- `SharpNinja.Avalonia.RemoteControl.Protocol`
- `SharpNinja.Avalonia.RemoteControl.Runtime`
- `SharpNinja.Avalonia.RemoteControl.Server`
- `SharpNinja.Avalonia.RemoteControl.Tool`

Pipelines:

- GitHub Actions for public validation and package artifacts.
- Azure Pipelines for private Azure DevOps validation and release gating.

Before the first package publish, the project must document source-of-truth and duplicate publish prevention across GitHub and Azure DevOps.
