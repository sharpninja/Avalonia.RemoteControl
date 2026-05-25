# Client Tool

`SharpNinja.Avalonia.RemoteControl.Tool` installs the `avalonia-remote` command.

## Install

```powershell
dotnet tool install --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.1.3
```

## Launch The Desktop Client

```powershell
avalonia-remote
```

The client supports:

- Local loopback connections.
- Network/TLS connections.
- ADB-forwarded Android connections.
- Tree rendering.
- Live screenshot rendering in a separate window when the debuggee enables frame streaming.
- Tree replica rendering in the live window for structural debugging.
- Pointer, wheel, keyboard, and text input forwarding when the debuggee enables remote input.
- Selected-node property inspection.
- Approved property mutation.
- Focus and click actions when the server enables remote actions.
- Bounded `ILogger` streaming with Debug, Information, Warning, and Error verbosity selections.
- Saving and forgetting endpoint/token/certificate profile state.
- Saving and forgetting the selected transport protocol.

## CLI Help

```powershell
avalonia-remote --help
```

## Connection Fields

- Endpoint: debuggee URI, such as `http://127.0.0.1:47100` or `https://host:47100`.
- Token: bearer token configured by the debuggee.
- Certificate: optional trusted server certificate file or accepted fingerprint for TLS.
- Transport: `grpc` for the server package transport, or `arc-protobuf-v1` for Android ADB bridge sessions.

## Saved Profiles

The desktop client can save a default connection profile in user-scoped application data. The profile includes endpoint, token, certificate trust, and transport protocol. Use Forget when the token, endpoint, transport, or certificate trust should no longer be retained.

Tokens must not be checked into source control, pasted into shared logs, or stored in production config.

Running `avalonia-remote adb connect --keep-forward` also saves the default profile for the desktop client. For an Android bridge marker, that profile uses `arc-protobuf-v1`.

## Working With The Tree

After a successful connection:

1. Select a node in the tree.
2. Inspect bounds, visibility, enabled state, focus state, names, classes, and properties.
3. Use refresh or live updates to keep the tree current.
4. Retry after stale-node errors because controls can be recreated by the app.

## Live View

After a successful connection, click Live View to open a separate remote UI window.

The live window has two modes:

- Screenshot: renders the streamed PNG frames from the debuggee. This is the default mode and gives the closest visual match.
- Tree Replica: renders the latest streamed control tree using absolute control bounds. Use this mode to inspect layout, labels, focus, hover, selection, and hit-test structure.

The overlay checkbox draws the latest tree bounds over either mode. Pointer, wheel, keyboard, and text input are sent in root-relative DIPs so the same live window works with gRPC and Android bridge sessions.

Live screenshots require `AllowRemoteFrames = true` on the debuggee. Remote input requires both `AllowRemoteActions = true` and `AllowRemoteInput = true`. If those gates are disabled, the client keeps the connection open but shows sanitized failures instead of sending privileged operations.

## Actions And Properties

Click/focus actions require `AllowRemoteActions = true` on the server.

Property edits require the property to be public, settable, supported by the value converter, not sensitive, and allowed by server policy.

If an operation is blocked, the client should show a sanitized failure reason. Check the debuggee logs for the matching audit event.

## Logs

Click Logs to start or stop streaming `ILogger` entries. The Verbosity setting chooses the minimum level requested from the debuggee: Debug, Information, Warning, or Error. The default is Information.
