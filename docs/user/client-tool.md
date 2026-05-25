# Client Tool

`SharpNinja.Avalonia.RemoteControl.Tool` installs the `avalonia-remote` command.

## Install

```powershell
dotnet tool install --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.1.2
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
- Selected-node property inspection.
- Approved property mutation.
- Focus and click actions when the server enables remote actions.
- Bounded `ILogger` streaming.
- Saving and forgetting endpoint/token/certificate profile state.

## CLI Help

```powershell
avalonia-remote --help
```

## Connection Fields

- Endpoint: debuggee URI, such as `http://127.0.0.1:47100` or `https://host:47100`.
- Token: bearer token configured by the debuggee.
- Certificate: optional trusted server certificate file or accepted fingerprint for TLS.
- Mode: Local, Network, or ADB.

## Saved Profiles

The desktop client can save a default connection profile in user-scoped application data. Use Forget when the token, endpoint, or certificate trust should no longer be retained.

Tokens must not be checked into source control, pasted into shared logs, or stored in production config.

## Working With The Tree

After a successful connection:

1. Select a node in the tree.
2. Inspect bounds, visibility, enabled state, focus state, names, classes, and properties.
3. Use refresh or live updates to keep the tree current.
4. Retry after stale-node errors because controls can be recreated by the app.

## Actions And Properties

Click/focus actions require `AllowRemoteActions = true` on the server.

Property edits require the property to be public, settable, supported by the value converter, not sensitive, and allowed by server policy.

If an operation is blocked, the client should show a sanitized failure reason. Check the debuggee logs for the matching audit event.
