# Avalonia.RemoteControl User Docs

Avalonia.RemoteControl helps developers and QA engineers inspect and control a running Avalonia app from a separate desktop client.

Current published packages:

- `SharpNinja.Avalonia.RemoteControl.Protocol`
- `SharpNinja.Avalonia.RemoteControl.Runtime`
- `SharpNinja.Avalonia.RemoteControl.Server`
- `SharpNinja.Avalonia.RemoteControl.Tool`

The client command is:

```powershell
avalonia-remote
```

## Start Here

- [Getting Started](getting-started.md) - install the tool, add the server package, and connect to a local app.
- [Server Integration](server-integration.md) - configure an Avalonia app as a debuggee.
- [Client Tool](client-tool.md) - launch the desktop client and use the CLI workflows.
- [Android ADB](android-adb.md) - connect to an Android emulator or connected device through `adb`.
- [Security](security.md) - understand the default safety model before enabling remote control.
- [Troubleshooting](troubleshooting.md) - diagnose common startup, connection, ADB, and package issues.

## Current Capabilities

- Inspect the current Avalonia control tree.
- Inspect safe public properties and state.
- Receive live tree refreshes.
- Invoke focus and click actions when enabled.
- Set approved public properties when enabled by policy.
- Stream bounded, redacted `ILogger` data.
- Connect over loopback, TLS network endpoints, or ADB forwarding.

## Current Limits

- Remote control is designed for debugging, QA, and controlled diagnostics, not production user administration.
- The desktop server package uses ASP.NET Core/Kestrel gRPC and is not the Android app-side transport.
- Android app-side support uses the runtime bridge package and a package-private marker file.
- Property mutation is deny-by-default and must be explicitly allowed by the debuggee app.
- Text input, drag/drop, arbitrary method calls, and private-field access are not v1 capabilities.
