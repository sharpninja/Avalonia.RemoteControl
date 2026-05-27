# Settings Guide

Avalonia.RemoteControl has two groups of settings:

- Debuggee settings: configured in the app that exposes remote control.
- Client settings: entered or saved by the desktop tool or ADB command line.

The safest way to configure a session is to start read-only, then enable one privileged surface at a time. Tree snapshots and logs are useful for diagnosis without allowing mutation. Property edits, click/focus actions, live screenshots, and remote input should be enabled only for a controlled debugging session.

## Debuggee Settings

Debuggee settings are supplied through `AvaloniaRemoteControlOptions` when the app calls `AddAvaloniaRemoteControl` or `AddAvaloniaRemoteControlRuntime`.

```csharp
services.AddAvaloniaRemoteControl(options =>
{
    options.IsEnabled = true;
    options.Port = 47100;
    options.AuthenticationToken = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TOKEN");
});
```

For Android bridge hosts, use `AddAvaloniaRemoteControlRuntime` and start `RemoteControlBridgeTcpListener` instead of the desktop Kestrel server package.

## Enablement

`IsEnabled`

This is the master switch. When false, the remote-control transport should not expose a listener. Keep it false by default and turn it on from a development-only setting such as an environment variable, debug build flag, or local developer configuration.

Recommended pattern:

```csharp
options.IsEnabled =
    Environment.GetEnvironmentVariable("AVALONIA_REMOTE_ENABLED") == "1";
```

Do not infer enablement from the presence of a token alone. A token in the environment is not the same as an explicit decision to expose the app.

## Listener

`Host`

The IP address the listener binds to. The default is `IPAddress.Loopback`, which means the server is reachable only from the same machine. Use loopback for normal local debugging and for ADB bridge listeners inside Android app processes.

Use a non-loopback address only when another machine must connect directly. Non-loopback listeners are treated as network exposure and require TLS by default.

`Port`

The TCP port used by the remote-control listener. The default protocol port is `47100`. Change it when the port is already in use or when you run multiple debuggee apps at the same time.

When using ADB forwarding, there are two ports:

- Device port: the port the Android app listens on inside the device or emulator.
- Host port: the port exposed on the development machine by `adb forward`.

They can be the same, but they do not have to be.

## Authentication

`RequireAuthentication`

Controls whether bearer authentication is required. The default is true. Leave it true for every real debugging session, including loopback and ADB sessions.

`AuthenticationToken`

The bearer token clients must present. This is required when authentication is enabled. Generate a high-entropy value for each debugging session and pass it through an environment variable, user secret, or local secret store.

Do not put this value in:

- Source code
- Project files
- Package artifacts
- Screenshots
- Issue comments
- Logs

`AuthenticatedClientIdentity`

The sanitized identity recorded in audit logs after successful authentication. The default is `remote-client`. Use a value that identifies the tool or operator without including secrets, for example `desktop-client`, `qa-laptop`, or `adb-session`.

This value is for diagnostics and audit readability. It is not a replacement for authentication.

## TLS And Cleartext

`RequireTlsForNonLoopback`

The default is true. It blocks non-loopback cleartext listeners unless TLS is configured. Keep it true unless you are writing a local-only test harness.

`TlsCertificatePath`

Path to the certificate used by a non-loopback HTTPS listener. This is required for LAN or remote network debugging when TLS is required.

`TlsCertificatePassword`

Password for the TLS certificate file. Supply it through a secret source. Do not hardcode it.

`AllowCleartextForLoopbackOrAdb`

The default is true. It allows cleartext HTTP/2 only for loopback and explicit ADB tunnel sessions. This keeps local developer setup simple while still rejecting unsafe LAN cleartext by default.

Set it false if your organization requires TLS even for local debugging. When false, a TLS certificate path is required.

`IsAdbTunnel`

Marks the listener as reachable only through an explicit ADB localhost tunnel. This is useful for Android bridge hosts because the app binds to loopback inside the device and the desktop connects through `adb forward`.

This setting does not remove authentication. ADB forwarding controls reachability; bearer authentication still controls access.

## Read-Only Inspection

Tree snapshots, live tree streaming, and property inspection are the least privileged features. They still expose app structure and selected public state, so they require authentication and redaction, but they do not directly mutate app state.

The root provider determines what part of the app is visible. Prefer returning the current `TopLevel`, `Window`, or application root control. The runtime normalizes child roots to their containing `TopLevel` for frame capture, tree snapshots, and input dispatch when possible.

## Property Mutation

`DenyPropertyMutationByDefault`

The default is true. Keep it true. Property mutation should be opt-in per property, not broadly enabled.

`AllowedMutableProperties`

The allow-list for property edits. Supported forms are:

- Property name, for example `Text`
- Type name plus property, for example `TextBox.Text`
- Full type name plus property, for example `Avalonia.Controls.TextBox.Text`

Start with the most specific entry that supports the workflow. For example, prefer `TextBox.Text` over `Text` if only text boxes should be editable.

Sensitive names are still blocked by the redaction policy even if allow-listed. Do not allow-list credential, token, key, password, or connection string properties.

## Remote Actions

`AllowRemoteActions`

Enables action-style operations such as focus and click. The default is false.

Turn it on only when the operator needs to interact with controls. When false, the client can still inspect the tree and logs.

Remote input also depends on this setting. `AllowRemoteInput = true` is not enough by itself.

## Live Screenshots

`AllowRemoteFrames`

Enables live PNG frame streaming for the live-view panel, whether it is docked or hosted by a generic floating tool window. The default is false.

Frame streaming exposes what the user would see on the target app surface. That may include business data, PII, credentials accidentally shown in UI, map positions, or other sensitive state. Enable it only when the debugging operator is trusted to view the app.

`FrameStreamInterval`

Controls how frequently live frames are captured. The default is 100 ms, roughly 10 frames per second.

Lower values feel more responsive but cost more CPU, memory bandwidth, and network throughput. Higher values reduce overhead and are often enough for layout inspection.

Typical choices:

- `TimeSpan.FromMilliseconds(100)`: interactive debugging
- `TimeSpan.FromMilliseconds(250)`: lower-overhead visual inspection
- `TimeSpan.FromSeconds(1)`: occasional visual confirmation

`MaxFramePixelCount`

Maximum captured frame size in pixels. The default is 4,000,000 pixels. This guard prevents accidentally streaming very large surfaces.

If frame streaming fails on a high-DPI or large-window target, compare the rendered pixel size to this limit before increasing it. Raising the limit increases memory and transport cost.

## Remote Input

`AllowRemoteInput`

Enables pointer, wheel, keyboard, and text input forwarding from the live-view panel. The default is false.

Remote input requires both:

```csharp
options.AllowRemoteActions = true;
options.AllowRemoteInput = true;
```

This two-gate model is intentional. Remote input is more powerful than a semantic click command because it can type text, drag through UI, scroll, and interact with transient popups or flyouts.

Input coordinates are root-relative DIPs. The client maps the live window image or tree replica back to the target root coordinate space before sending input.

Audit logs should record sanitized input metadata such as event count and result. They must not record typed text payloads.

## Live Tree Streaming

`TreeStreamInterval`

Controls how frequently the debuggee emits live tree snapshots. The default is 250 ms.

Tree streaming is useful for structural debugging, selection overlays, and the live-view tree replica mode. It is cheaper than frame streaming, but it still walks the UI tree and serializes node state.

Raise the interval when:

- The target UI is very large.
- The app is running on constrained hardware.
- You need lower overhead more than fast updates.

Lower the interval when:

- You are debugging rapid layout changes.
- Selection state or enabled/visible state changes need to appear quickly.

## Logging

`LogBufferCapacity`

The number of sanitized log entries retained for new log stream subscribers. The default is 1024.

The buffer is bounded. If more entries arrive than it can retain, older entries are dropped and clients see a dropped-message count. Increase the value for noisy apps, but remember that every retained entry consumes memory.

`SensitiveNameFragments`

Name fragments that cause property and log data to be redacted. Defaults include:

- `password`
- `token`
- `secret`
- `key`
- `credential`
- `auth`
- `cookie`
- `connection string`

Add project-specific terms before exposing a real app. Examples include internal customer identifiers, API names, tenant labels, or product-specific credential names.

Debug protocol event logging

The runtime writes Debug `ILogger` messages for protocol events sent to and from the client. These messages are intended for diagnosing the remote-control system itself. They avoid bearer tokens, property values, and typed text.

`WatchLogs` logs stream lifecycle events, but it does not re-log every outgoing log entry. Re-logging every log entry would recursively create more log entries when Debug streaming is enabled.

## Client Connection Settings

Endpoint

The URI the client connects to. Use:

- `http://127.0.0.1:47100` for loopback or ADB-forwarded cleartext.
- `https://host-or-ip:47100` for TLS network endpoints.

For ADB workflows, the endpoint is normally saved automatically after `avalonia-remote adb connect --keep-forward`.

Token

The bearer token configured by the debuggee. Paste only the token value, not the `Bearer ` prefix.

Certificate path

Optional certificate file used for TLS trust. Use this when the client should trust a development certificate directly from a file.

Accepted SHA-256 fingerprint

The certificate fingerprint accepted through Inspect Cert and Accept Cert. Fingerprint trust is exact-match trust. If the server certificate changes, inspect and accept the new certificate again.

Transport

The protocol used by the endpoint:

- `grpc`: desktop/server package transport.
- `arc-protobuf-v1`: Android bridge transport over a TCP forward.

If an ADB bridge profile opens as `grpc`, the connection will fail because the app-side bridge is not Kestrel gRPC.

Log verbosity

The minimum `ILogger` level requested by the client log stream. Choices are:

- Warning: default; keeps the initial log view focused on actionable warnings and errors.
- Debug: includes remote-control protocol event diagnostics and application Debug logs.
- Information: good for normal audit and app state messages.
- Warning: useful when the app is noisy and you only need problems.
- Error: useful for failure-only monitoring.

Changing verbosity restarts an active log stream with the new minimum level.

Log floating

The desktop client can open the current log view in a generic floating tool window. The floating panel owns the visible log list while it is open, so the main window shows that logs are floating instead of rendering the same rows twice. Dock returns the same shared log view model to the main window. The stream, verbosity, status text, entry count, and buffered rows are shared, so floating or docking does not restart streaming or duplicate remote log requests.

Save and Forget

Save stores the default endpoint, token, certificate trust, and transport protocol in user-scoped application data. Forget removes those saved settings from the client profile and clears the visible fields.

Do not use saved profiles as a team secret store. They are convenience state for one local user.

## ADB CLI Settings

`--serial`

Required for `adb connect` and `adb cleanup`. It selects the emulator or connected device. Use `avalonia-remote adb list` to find the serial.

`--package`

Package name used for marker discovery. The app must be debuggable because marker discovery uses `adb shell run-as`.

`--device-port`

Device-side listener port. Use this when marker discovery is unavailable or you already know the app bridge/server port.

`--host-port`

Host-side forwarded port. Defaults to 47100. Change it when that port is already used on the development machine.

`--token`

Bearer token to use for the forwarded endpoint. Required when marker discovery does not provide a token.

`--transport-protocol`

Explicit transport protocol for direct device-port connections. Supported values are `grpc` and `arc-protobuf-v1`. Package marker discovery normally supplies this value.

`--keep-forward`

Leaves the ADB forward open after a successful probe and saves a desktop client profile. Use this when you want to launch the desktop UI and click Connect after the CLI has prepared the tunnel.

Without `--keep-forward`, the CLI removes the forward after a successful probe.

`adb cleanup`

Removes a kept forward:

```powershell
avalonia-remote adb cleanup --serial <serial> --host-port 47100
```

## Desktop ADB Settings

ADB path

Optional path to `adb.exe`. Leave blank to use `AVALONIA_REMOTE_ADB_PATH`, PATH lookup, or a common Android SDK location.

Device

Selected emulator or physical device. Refresh Devices fills this list from `adb devices -l`.

Package

Android package name to launch and inspect with `run-as`. The package must be debuggable for marker discovery.

Host port

Host-side localhost port for `adb forward`. The default is 47100.

Android Connect

Launches the package when stopped, waits for a running process, reads the package-private remote-control marker, creates the forward, probes capabilities with bearer authentication, saves the desktop profile, and connects immediately.

Cleanup Forward

Removes `adb forward tcp:<hostPort>` for the selected serial.

## Recommended Profiles

Read-only local inspection:

```csharp
options.IsEnabled = true;
options.AuthenticationToken = token;
```

Interactive local debugging:

```csharp
options.IsEnabled = true;
options.AuthenticationToken = token;
options.AllowRemoteActions = true;
options.AllowRemoteFrames = true;
options.AllowRemoteInput = true;
```

Narrow property editing:

```csharp
options.IsEnabled = true;
options.AuthenticationToken = token;
options.AllowedMutableProperties.Add("TextBox.Text");
```

LAN debugging:

```csharp
options.IsEnabled = true;
options.Host = IPAddress.Parse("192.168.1.25");
options.AuthenticationToken = token;
options.TlsCertificatePath = "devcert.pfx";
options.TlsCertificatePassword = certPassword;
```

Android bridge:

```csharp
options.IsEnabled = true;
options.Host = IPAddress.Loopback;
options.Port = 47100;
options.IsAdbTunnel = true;
options.AuthenticationToken = token;
```

Android interactive live view:

```csharp
options.IsEnabled = true;
options.Host = IPAddress.Loopback;
options.Port = 47100;
options.IsAdbTunnel = true;
options.AuthenticationToken = token;
options.AllowRemoteActions = true;
options.AllowRemoteFrames = true;
options.AllowRemoteInput = true;
```

## Common Misconfigurations

Server starts but client cannot connect:

- Check `Host`, `Port`, and endpoint URI.
- Check whether the endpoint is `http` for loopback or `https` for TLS.
- Check that the token matches.
- For ADB, check the forward with `adb forward --list`.

Startup validation fails:

- `AuthenticationToken` is missing while authentication is required.
- `TlsCertificatePath` is missing for a non-loopback listener.
- Cleartext was disabled without a TLS certificate path.

Live view is blank:

- `AllowRemoteFrames` is false.
- The root provider returns null.
- The frame exceeds `MaxFramePixelCount`.
- The client is connected with the wrong transport.

Input does nothing:

- `AllowRemoteActions` is false.
- `AllowRemoteInput` is false.
- The live window is connected to a view-only endpoint.
- The target control is stale or not hit-testable.

Logs are missing:

- The app is not using the same `IServiceProvider` logging pipeline.
- The log stream is filtered above the event level.
- The client is disconnected or the log stream was stopped with the Logs button.
- The log entry was dropped because the buffer wrapped.
- The message was redacted because a sensitive fragment matched.
