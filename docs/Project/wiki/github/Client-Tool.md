# Client Tool

`SharpNinja.Avalonia.RemoteControl.Tool` installs the `avalonia-remote` command.

## Install

```powershell
dotnet tool install --global SharpNinja.Avalonia.RemoteControl.Tool
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
- Live screenshot rendering in a docked or floating live-view pane when the debuggee enables frame streaming.
- Tree replica rendering in the live window for structural debugging.
- Pointer, wheel, keyboard, and text input forwarding when the debuggee enables remote input.
- Selected-node property inspection.
- Approved property mutation.
- Focus and click actions when the server enables remote actions.
- Bounded `ILogger` streaming with Debug, Information, Warning, and Error verbosity selections.
- Saving and forgetting endpoint/token/certificate profile state.
- Saving and forgetting the selected transport protocol.
- Embedded terminal launch for Codex MCP sessions against the connected app.

For detailed behavior of each client field and server option, see [Settings Guide](Settings-Guide).

## CLI Help

```powershell
avalonia-remote --help
```

## Connection Fields

- Endpoint: debuggee URI, such as `http://127.0.0.1:47100` or `https://host:47100`.
- Token: bearer token configured by the debuggee.
- Certificate: optional trusted server certificate file or accepted fingerprint for TLS.
- Transport: `grpc` for the server package transport, or `arc-protobuf-v1` for Android ADB bridge sessions.

Endpoint, token, certificate, accepted fingerprint, transport, log verbosity, and saved profile behavior are explained in [Settings Guide](Settings-Guide).

## Android ADB Connect

Use the Android ADB row when the target app is running on an emulator or connected device. Refresh Devices lists attached targets, Android Connect launches the package if it is stopped, waits for the debug marker, creates the ADB forward, saves the discovered endpoint/token/transport profile, and connects the desktop client. Cleanup Forward removes the host-side forward for the selected device and host port.

If the package name is blank but a device is selected, the top Connect button can still prepare an explicit `arc-protobuf-v1` bridge forward. In that mode the client forwards the selected host port to the same device port and uses the token already entered in the Token field.

## Terminal And Codex MCP

The Workspace tab hosts the terminal panel. Click Codex MCP to launch the installed `codex` CLI with the running desktop client's in-process MCP server already configured as `avalonia_remote_control`.

The Codex MCP launch is self-contained:

- no `avalonia-remote mcp` child process is started;
- no remote endpoint, transport, bearer token, certificate path, or profile name is passed to Codex;
- Codex receives only the tool's loopback MCP URL and a seed prompt that explains the available tools;
- the working directory defaults to the CWD from when `avalonia-remote` was started, unless you edit Working Dir.

See [Codex MCP](Codex-MCP) for the tool list, prompt guidance, and troubleshooting.

## Saved Profiles

The desktop client can save a default connection profile in user-scoped application data. The profile includes endpoint, token, certificate trust, and transport protocol. Use Forget when the token, endpoint, transport, or certificate trust should no longer be retained.

Tokens must not be checked into source control, pasted into shared logs, or stored in production config.

Running `avalonia-remote adb connect --keep-forward` also saves the default profile for the desktop client. For an Android bridge marker, that profile uses `arc-protobuf-v1`.

## Projects, Sessions, And Replay Data

The desktop client maintains a default user-scoped project file. The project stores app connection profiles, connection sessions, log history, interaction journals, and replay artifacts as versioned JSON under the current user's application data folder.

Each successful connection starts a project session. During the session, the client records:

- the app/profile identity and transport settings used to connect;
- client status rows and streamed remote log entries;
- click, focus, property, and live-input interactions;
- tree snapshot artifacts around command interactions when a snapshot is available;
- replay metadata that can be used by the client replay service to run the same interaction sequence again.

Replay diffs compare the original captured tree state with the replayed tree state after each step. The first diff model reports added, removed, changed, and unchanged nodes from serialized control-tree snapshots. Pixel/frame diffs can be added later without changing the project/session foundation.

Replay files are local debugging artifacts. Typed text and property values needed for replay can be sensitive; they are marked as sensitive replay fields and must not be copied into `ILogger` diagnostic messages.

## Working With The Tree

After a successful connection:

1. Select a node in the tree.
2. Inspect bounds, visibility, enabled state, focus state, names, classes, and properties.
3. Use refresh or live updates to keep the tree current.
4. Retry after stale-node errors because controls can be recreated by the app.

## Live View

After a successful connection, click Live View to show the Live View pane, docked below Remote Tools on the right. Drag the Live View tab out (or use the pane's float control) to float it in its own window, and drag it back to re-dock; the floating and docked live views share the same rendering and behavior.

The live panel has two modes:

- Screenshot: renders the streamed PNG frames from the debuggee. This is the default mode and gives the closest visual match.
- Tree Replica: renders the latest streamed control tree using absolute control bounds. Use this mode to inspect layout, labels, focus, hover, selection, and hit-test structure.

Clicking a visible control in the live panel selects the matching node in the main Control Tree when the node is present in the latest tree snapshot. The selected live-view overlay outline is highlighted in gold.

The docked and floating live views use the same rendering, overlay, input, and tree-selection behavior. Floating a pane never disconnects the main client.

The overlay checkbox draws the latest tree bounds over either mode. Pointer, wheel, keyboard, and text input are sent in root-relative DIPs so the same live panel works with gRPC and Android bridge sessions.

Live screenshots require `AllowRemoteFrames = true` on the debuggee. Remote input requires both `AllowRemoteActions = true` and `AllowRemoteInput = true`. If those gates are disabled, the client keeps the connection open but shows sanitized failures instead of sending privileged operations.

## Actions And Properties

Click/focus actions require `AllowRemoteActions = true` on the server.

Property edits require the property to be public, settable, supported by the value converter, not sensitive, and allowed by server policy.

If an operation is blocked, the client should show a sanitized failure reason. Check the debuggee logs for the matching audit event.

## Logs

The client starts streaming `ILogger` entries after a successful connection. Click Logs to stop or restart the stream. The Verbosity setting chooses the minimum level requested from the debuggee: Debug, Information, Warning, or Error. The default is Warning to keep the initial log view focused on actionable entries; switch to Debug when you need remote-control protocol diagnostics.

Changing verbosity restarts an active stream with the new minimum level. The log header shows whether the stream is active, how many entries are displayed, and any stream failure.

The Logs pane is docked at the bottom of the workspace. Drag its tab out (or use the pane's float control) to float it in its own window, and drag it back to re-dock. Floating and docking share the same log view model and never start a second stream, and the Verbosity selector continues to control that shared stream.

## Project Tab

The right-side Project tab shows the active client project, project storage root, saved app profiles, session count, active session metadata, log count, interaction count, replay artifact count, and whether the current session has replayable steps. The project file is saved automatically as sessions record logs and interactions; Save Project forces an immediate write.

## Dockable Panels

The desktop client docks its panels with [Dock.Avalonia](https://github.com/wieslawsoltes/Dock), styled to match a Visual Studio dark shell: a compact command bar, dark input fields, and dockable tool panes with pin, float, and close controls on their headers. The Control Tree docks to the left, the Workspace document (Terminal and Properties tabs) fills the center above the Logs pane, and Remote Tools sits above Live View on the right.

Drag a pane's tab or header to rearrange it, float it into its own window, or dock it elsewhere, then drag it back to return it to the shell. The full dock layout (pane arrangement, proportions, and floating windows) is saved per project and restored on the next launch, alongside the window size, selected tabs, and docked live-view preference. If the saved layout had live view docked, the client restores that dock after the next successful connection because live streaming needs an active remote session.
