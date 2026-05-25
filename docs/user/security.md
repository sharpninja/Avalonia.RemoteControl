# Security Guide

Avalonia.RemoteControl exposes a debugging and mutation surface. Treat every enabled session as sensitive.

## Default Posture

- Disabled by default.
- Bearer authentication required.
- Loopback binding by default.
- TLS required for non-loopback listeners.
- ADB tunnel sessions still require authentication.
- Property mutation denied by default.
- Remote actions disabled by default.
- Live frame streaming disabled by default.
- Remote input disabled by default.
- Sensitive properties and log data redacted by default.

## Enable Only In Controlled Builds

Recommended pattern:

```csharp
options.IsEnabled =
    Environment.GetEnvironmentVariable("AVALONIA_REMOTE_ENABLED") == "1";
options.AuthenticationToken =
    Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TOKEN");
```

Do not enable remote control by default in production builds.

## Tokens

- Generate high-entropy tokens for each debugging session.
- Pass tokens through environment variables, user secrets, or a secure local secret store.
- Do not log tokens.
- Do not write tokens into screenshots, issue comments, package artifacts, or source control.
- Rotate tokens after a debugging session.

## Network Access

Use loopback for normal local debugging:

```text
http://127.0.0.1:47100
```

Use TLS for non-loopback debugging:

```text
https://192.168.1.25:47100
```

The client can trust a development certificate by file or accepted fingerprint. Fingerprint trust is exact-match SHA-256 trust; if the server certificate changes, reconnect with updated trust material.

## Mutation And Actions

Keep mutation allow-lists narrow:

```csharp
options.AllowedMutableProperties.Add("Text");
options.AllowedMutableProperties.Add("TextBox.Text");
```

Enable actions only when needed:

```csharp
options.AllowRemoteActions = true;
```

Enable live view surfaces only for controlled debugging sessions:

```csharp
options.AllowRemoteFrames = true;
options.AllowRemoteActions = true;
options.AllowRemoteInput = true;
```

Frame streaming can expose visible application data. Remote input can manipulate application state and can include typed text. Audit logs must record sanitized input metadata, but must not record typed text payloads.

Every allowed or rejected mutation/action should be visible through sanitized audit logs.

## Redaction

Default redaction blocks names containing:

- `password`
- `token`
- `secret`
- `key`
- `credential`
- `auth`
- `cookie`
- `connection string`

Add project-specific sensitive fragments before enabling remote control in a real app.

## ADB

ADB forwarding only makes the debuggee reachable on the host machine. It does not replace authentication. Keep the Android bridge listener bound to loopback inside the app process.

## Checklist

- Remote control is disabled in production.
- Token comes from a secret source.
- Listener is loopback unless TLS is configured.
- Non-loopback TLS certificate is configured and trusted by the client.
- Mutation allow-list is minimal.
- Remote actions are enabled only for workflows that need them.
- Frame streaming is enabled only when screenshots are acceptable for the debugging session.
- Remote input is enabled only when the operator is trusted to interact with the debuggee.
- Logs and property snapshots redact app-specific sensitive names.
- ADB marker files are written only for debuggable builds.
