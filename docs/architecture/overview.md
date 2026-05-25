# Architecture Overview

## System Shape

Avalonia.RemoteControl has four major parts:

- Server SDK embedded into the debuggee app.
- Versioned gRPC protocol contracts.
- Desktop client UI launched through a .NET tool.
- Android/ADB connection support for emulator and attached-device apps.

## Server SDK

The server SDK will be packaged as `Avalonia.RemoteControl.Server`.

Implemented responsibilities:

- register services through `IServiceCollection`
- capture Avalonia tree snapshots through the UI dispatcher
- expose safe property metadata and mutations
- invoke supported actions
- capture `ILogger` output through a bounded provider
- enforce disabled-by-default startup state, redaction, and deny-by-default mutation/action gates

Planned responsibilities:

- start the transport through `IServiceProvider`/app lifetime integration
- enforce bearer authentication and TLS/listener policy on hosted RPCs
- emit authenticated audit records for security and command decisions

## Protocol

The desktop-facing protocol is versioned gRPC.

Defined RPCs:

- `GetCapabilities`
- `GetSnapshot`
- `WatchTree`
- `InvokeClick`
- `SetProperty`
- `WatchLogs`

The protocol must support version/capability negotiation because Android transport feasibility may force different app-side transport internals while preserving the desktop-facing contract.

## Client

The client is an Avalonia desktop application launched by the .NET tool package `Avalonia.RemoteControl.Tool`.

Command:

```powershell
avalonia-remote
```

Planned client areas:

- connection profile screen
- visual/control tree view
- selected-node details
- property inspector/editor
- action controls
- log viewer
- connection/audit status

## Android

Android connectivity is a first-class product requirement. The client should automate ADB device selection, forwarding, package/endpoint discovery, connection, and cleanup.

The Android app-side transport is not yet proven. Technical Spike 0 must decide whether Android can host the selected transport in-process or needs a bridge transport behind the desktop-facing gRPC API.

## Current Implementation Status

- `Avalonia.RemoteControl.Protocol` defines the versioned gRPC contract.
- `Avalonia.RemoteControl.Server` captures stable tree snapshots, streams snapshots, exposes guarded actions/property mutation, and captures sanitized logs through a bounded `ILoggerProvider`.
- `Avalonia.RemoteControl.Tool` is a packaged command stub with Local, Network, and ADB workflow help.
- CI files exist for GitHub Actions and Azure Pipelines.
- Android ADB transport, hosted server startup, authentication middleware, TLS binding, and full desktop client UI remain future slices.

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

See `docs/architecture/security-model.md`.

## Release Shape

Packages:

- `Avalonia.RemoteControl.Server`
- `Avalonia.RemoteControl.Tool`

Pipelines:

- GitHub Actions for public validation and package artifacts.
- Azure Pipelines for private Azure DevOps validation and release gating.

Before the first package publish, the project must document source-of-truth and duplicate publish prevention across GitHub and Azure DevOps.
