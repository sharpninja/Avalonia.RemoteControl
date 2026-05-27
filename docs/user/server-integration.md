# Server Integration

Use `SharpNinja.Avalonia.RemoteControl.Server` for desktop or server-capable Avalonia targets. Use `SharpNinja.Avalonia.RemoteControl.Runtime` for Android app-side bridge hosts.

## Package Selection

- Desktop debuggee: install `SharpNinja.Avalonia.RemoteControl.Server`.
- Android debuggee bridge: install `SharpNinja.Avalonia.RemoteControl.Runtime`.
- Custom protocol clients or adapters: reference `SharpNinja.Avalonia.RemoteControl.Protocol`.

## Required App Services

The server needs:

- `AddAvaloniaRemoteControl(...)` registration.
- An `IRemoteControlRootProvider` that returns the current root `Control`.
- Startup and shutdown through `StartAvaloniaRemoteControlAsync`, `StopAvaloniaRemoteControlAsync`, or `AttachAvaloniaRemoteControl`.
- A bearer token when `IsEnabled` is true.

## Configuration Options

Common options:

- `IsEnabled`: opens the remote-control transport when true.
- `Host`: listener IP address. Default is loopback.
- `Port`: listener port. Default is the remote-control protocol port.
- `AuthenticationToken`: bearer token expected from clients.
- `AuthenticatedClientIdentity`: sanitized name used in audit logs.
- `RequireTlsForNonLoopback`: blocks non-loopback cleartext by default.
- `TlsCertificatePath`: certificate path for TLS listeners.
- `TlsCertificatePassword`: optional certificate password.
- `AllowRemoteActions`: enables focus and click actions.
- `AllowRemoteFrames`: enables live PNG frame streaming. Default is false.
- `AllowRemoteInput`: enables pointer, wheel, keyboard, and text input forwarding when `AllowRemoteActions` is also true. Default is false.
- `DenyPropertyMutationByDefault`: keeps property mutation deny-by-default.
- `AllowedMutableProperties`: property allow-list for mutation.
- `TreeStreamInterval`: live tree stream refresh interval.
- `FrameStreamInterval`: live frame stream refresh interval. Default is 100 ms.
- `MaxFramePixelCount`: maximum captured frame pixel count before frame streaming is rejected.
- `LogBufferCapacity`: retained log entries for new subscribers.
- `SensitiveNameFragments`: default redaction fragments.

See [Settings Guide](settings.md) for the full explanation of defaults, recommended profiles, security tradeoffs, client fields, ADB flags, and common misconfigurations.

## Mutation Policy

Property mutation is denied unless one of these policy entries matches:

- Property name, for example `Text`
- Type name plus property, for example `TextBox.Text`
- Full type name plus property, for example `Avalonia.Controls.TextBox.Text`

Sensitive names are still blocked by redaction policy even if allow-listed. Names containing fragments such as `password`, `token`, `secret`, `key`, `credential`, `auth`, `cookie`, or `connection string` are redacted by default.

## Live Frames And Input

Live frame streaming and interactive input are separate debug-only gates:

```csharp
options.AllowRemoteFrames = true;
options.AllowRemoteActions = true;
options.AllowRemoteInput = true;
```

Frame streaming captures the Avalonia root with `RenderTargetBitmap` and sends PNG frames to connected clients. Remote input is delivered in root-relative DIPs and is rejected unless both action and input gates are enabled.

Use `FrameStreamInterval` and `MaxFramePixelCount` to tune live screenshot cost. Use `TreeStreamInterval` to tune structural update frequency for the live tree replica and overlay.

## Logging

`AddAvaloniaRemoteControl` adds a bounded `ILoggerProvider`. Connected clients can stream logs with:

- Timestamp
- Level
- Category
- Event ID
- Rendered message
- Structured state summary
- Scope summary
- Exception summary
- Sequence and dropped-message count

The provider does not replace your existing logging providers.

## Non-Loopback TLS

For LAN or remote network debugging, use TLS:

```csharp
options.IsEnabled = true;
options.Host = IPAddress.Parse("192.168.1.25");
options.Port = 47100;
options.AuthenticationToken = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TOKEN");
options.TlsCertificatePath = "devcert.pfx";
options.TlsCertificatePassword = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_CERT_PASSWORD");
```

The client can trust the presented certificate through an explicit certificate file or an accepted SHA-256 fingerprint. Do not disable TLS for non-loopback listeners.

## Manual Start And Stop

If you do not use Avalonia lifetime integration, start and stop the host explicitly:

```csharp
await services.StartAvaloniaRemoteControlAsync();
await services.StopAvaloniaRemoteControlAsync();
```

Call stop during application shutdown so the listener closes cleanly.
