# Functional Requirements (MCP Server)

## FR-ACTION-001 A connected client can invoke a basic click on an approved clickable target.

A connected client can invoke a basic click on an approved clickable target.
Scope: layer-1+

## FR-ACTION-002 A connected client can request focus for a focusable target.

A connected client can request focus for a focusable target.
Scope: layer-1+

## FR-ACTION-003 Stale node IDs produce a recoverable client error and trigger a refresh path.

Stale node IDs produce a recoverable client error and trigger a refresh path.
Scope: layer-1+

## FR-ACTION-004 Unsupported actions are reported as unsupported with a reason.

Unsupported actions are reported as unsupported with a reason.
Scope: layer-1+

## FR-ACTION-005 Remote pointer and keyboard input

A connected client can send approved pointer, wheel, keyboard, and text input to the remote application from the live view.
Scope: layer-1+

## FR-ADB-001 The client can discover connected Android emulators/devices through `adb`.

The client can discover connected Android emulators/devices through `adb`.
Scope: layer-1+

## FR-ADB-002 The client can connect to an Avalonia app running on Android without the user manually typing port-forward commands.

The client can connect to an Avalonia app running on Android without the user manually typing port-forward commands.
Scope: layer-1+

## FR-ADB-003 The client supports selecting a specific device/emulator when multiple ADB targets are connected.

The client supports selecting a specific device/emulator when multiple ADB targets are connected.
Scope: layer-1+

## FR-ADB-004 The client can connect by package name, explicit endpoint, or discovered debug marker.

The client can connect by package name, explicit endpoint, or discovered debug marker.
Scope: layer-1+

## FR-ADB-005 The client tears down ADB forwarding when the session ends unless the user asks to keep it.

The client tears down ADB forwarding when the session ends unless the user asks to keep it.
Scope: layer-1+

## FR-ADB-006 The client exposes equivalent ADB workflows through CLI commands.

The client exposes equivalent ADB workflows through CLI commands.
Scope: layer-1+

## FR-ADB-007 Integrated desktop ADB connection workflow

The desktop client provides an integrated ADB connection workflow so users can list devices, launch a selected Android package when needed, create the ADB forward, save the transport-aware profile, and connect without running an external script.
Scope: layer-1+

## FR-ANDROID-001 Embedded Android device control MCP tools

The desktop client MCP server must expose self-contained Android device and emulator control tools so an AI agent can list devices, manage AVDs, launch emulators, install and launch apps, manage forwards, collect logcat, inspect UI hierarchy, capture screenshots, and send basic input without requiring a separate Android MCP dependency.
Scope: layer-1+

## FR-ANDROID-002 Android log stream liveness

The client must keep Android-backed remote-control log streaming alive or recover it when idle service polling would otherwise time out, and must surface a clear recoverable status instead of treating the failure as an app crash.
Scope: layer-1+

## FR-CLIENT-001 The client provides Local, Network, and ADB connection modes.

The client provides Local, Network, and ADB connection modes.
Scope: layer-1+

## FR-CLIENT-002 The client renders the remote tree and selected-node details.

The client renders the remote tree and selected-node details.
Scope: layer-1+

## FR-CLIENT-003 The client provides a property inspector/editor for selected nodes.

The client provides a property inspector/editor for selected nodes.
Scope: layer-1+

## FR-CLIENT-004 The client provides action controls for supported remote interactions.

The client provides action controls for supported remote interactions.
Scope: layer-1+

## FR-CLIENT-005 The client provides a log viewer.

The client provides a log viewer.
Scope: layer-1+

## FR-CLIENT-006 The client shows connection state, authentication state, and transport mode.

The client shows connection state, authentication state, and transport mode.
Scope: layer-1+

## FR-CLIENT-008 Separate live remote view window

The desktop client must open a separate live remote UI window that can render the remote application outside the tree/property inspector.
Scope: layer-1+

## FR-CLIENT-009 Continuous live remote rendering

The live remote view must respond to changes in the remote UI without manual refresh.
Scope: layer-1+

## FR-CLIENT-010 Live view renders top-level visual surface

The live remote view must render the same top-level visual surface a user sees on the target device, including application background, popup, flyout, and overlay layers when Avalonia exposes them.
Scope: layer-1+

## FR-CLIENT-011 Live view click selects control tree node

When a user clicks a rendered control in the live remote UI window, the desktop client selects the corresponding node in the main control tree when that node is present in the current tree model.
Scope: layer-1+

## FR-CLIENT-012 Docked live remote UI

The desktop client allows the live remote UI view to be docked on the right side of the main client window while preserving the generic floating tool-window option; the docked Live View tab hosts the live-view panel directly without nested tool-window chrome and constrains or scrolls the surface inside the available tab space.
Scope: layer-1+

## FR-CLIENT-013 Visual Studio-style dockable panel chrome

The desktop client must provide Visual Studio-style tool-window chrome with visible icon commands for dock, float, auto-hide or pin, and close or hide actions; docked panels, pop-out windows, and dock commands must clearly separate draggable headers, content regions, active docked surfaces, and docking commands.
Scope: layer-1+

## FR-CLIENT-014 Project-scoped app connection settings

The desktop client stores connection settings by project and app so users can reconnect to local, network, or ADB debug targets without re-entering transport-specific settings.
Scope: layer-1+

## FR-CLIENT-015 Session log history per project

Each project records connection sessions with metadata and log history so previous debugging sessions can be reviewed after disconnecting.
Scope: layer-1+

## FR-CLIENT-016 Interaction recording for reproducible sessions

The client records remote-control interactions in a replayable session journal, including commands, timing, target context, and before/after state references needed to reproduce the debugging flow.
Scope: layer-1+

## FR-CLIENT-017 Replay and diff recorded sessions

The client can replay recorded interactions against a connected app and produce a per-step diff showing how the replayed app state differs from the original captured state.
Scope: layer-1+

## FR-CLIENT-018 Undock docked live view

When the live view is docked in the main client, the control command is labeled Undock and moves the live view back into a separate live-view window instead of simply closing the live-view surface.
Scope: layer-1+

## FR-CLIENT-019 Visual Studio 2026 client shell styling

The desktop client uses Visual Studio 2026-like dark shell styling across command bars, status bar, tool windows, tabs, dock headers, buttons, text inputs, lists, and pop-out windows.
Scope: layer-1+

## FR-CLIENT-020 Persistent client layout state

The desktop client remembers layout state such as window size, split sizes, selected right-side tab, log pop-out ownership, and docked live-view preference and restores that state on startup.
Scope: layer-1+

## FR-CLIENT-021 Interactive Visual Studio docking panels

Dockable client panels must expose interactive Visual Studio-like behavior, including draggable headers, icon-based panel management, floating pop-out windows, docking back into the shell, close or hide commands, persisted dock state, and clear user feedback for drag/drop dock targets.
Scope: layer-1+

## FR-CLIENT-022 Embedded terminal panel

The desktop client hosts an embedded terminal panel that can launch Codex or another configured command inside the remote-control shell, and the Codex AI agent starts in the same working directory that was current when the tool process was launched unless the user explicitly edits the terminal working directory field.
Scope: layer-1+

## FR-CLIENT-023 Terminal process lifecycle controls

The terminal panel must let users launch, interact with, and stop the configured CLI process without blocking the client UI or losing dock-panel behavior.
Scope: layer-1+

## FR-CLIENT-024 Tool-side MCP host for Codex control

The desktop tool exposes an in-process Model Context Protocol host enabled by default so Codex or another MCP client can inspect and invoke approved remote-control operations against the currently configured debug target without launching a second avalonia-remote child process; the embedded Codex preset seeds guidance that explains the exposed tools, directs Codex to inspect the control tree first, and forbids screenshots as the primary control-selection mechanism.
Scope: layer-1+

## FR-DOCKING-001 The desktop tool docks its UI panels using Dock.Avalonia 12 instead of a hand-rolled dock.

Replace the hand-rolled DockLayout/DockPaneChrome/FloatingDockPaneWindow with Dock.Avalonia 12, hosting Control Tree, Workspace, Remote Tools, Live View, and Logs in a DockControl via an MVVM factory, with per-project layout persistence.
Scope: layer-1+
**Acceptance Criteria:**
- [ ] Panels are hosted in a Dock.Avalonia DockControl via an MVVM factory.
- [ ] Legacy custom dock types (DockLayout, DockPaneChrome, FloatingDockPaneWindow) are removed.
- [ ] Docked layout persists per project and restores on next launch.

## FR-LOG-001 A connected client can stream `ILogger` events from the debuggee.

A connected client can stream `ILogger` events from the debuggee.
Scope: layer-1+

## FR-LOG-002 Log output includes timestamp, level, category, event ID, message, structured state summary, scope summary, exception summary, sequence number, and dropped-message count.

Log output includes timestamp, level, category, event ID, message, structured state summary, scope summary, exception summary, sequence number, and dropped-message count.
Scope: layer-1+

## FR-LOG-003 The client can filter logs by level and category.

The client can filter logs by level and category.
Scope: layer-1+

## FR-LOG-004 The client shows when logs were dropped because of buffering/backpressure.

The client shows when logs were dropped because of buffering/backpressure.
Scope: layer-1+

## FR-LOG-005 Client log verbosity setting

The client exposes a log verbosity setting with Debug, Information, Warning, and Error options and uses the selected level when streaming ILogger events.
Scope: layer-1+

## FR-LOG-006 Debug protocol event logging

Every remote-control operation sent from the client and every non-logstream response or stream update sent to the client emits a sanitized Debug ILogger message.
Scope: layer-1+

## FR-LOG-007 Client log stream visibility

The desktop client starts log streaming after connection by default, shows current log stream state and entry count, and displays log-stream failures inline instead of only in a transient global status message.
Scope: layer-1+

## FR-LOG-008 Pop-out log viewer

The desktop client allows the current log stream to be opened in a separate log window while keeping the main client connected and streaming.
Scope: layer-1+

## FR-LOG-009 Undocked log panel

When the desktop client opens logs in a pop-out window, the main window must remove the embedded log list and expose a clear way to dock the popped-out logs back into the main window.
Scope: layer-1+

## FR-LOG-010 Pop-out log verbosity control

The pop-out log window exposes the same Debug, Information, Warning, and Error verbosity selector as the main log panel and changing either selector updates the active log stream setting.
Scope: layer-1+

## FR-MCP-001 aiUnit MCP Server validation

The repository provides SharpNinja.aiUnit-backed integration tests that can validate the active MCP Server marker, health nonce behavior, plugin contract, and requirements tooling evidence when the live aiUnit review gate is explicitly enabled.
Scope: layer-1+

## FR-PROP-001 A connected client can inspect safe readable public CLR properties and Avalonia properties for a selected node.

A connected client can inspect safe readable public CLR properties and Avalonia properties for a selected node.
Scope: layer-1+

## FR-PROP-002 Property output identifies property name, declaring type, value representation, value type, read/write status, source category, and redaction status.

Property output identifies property name, declaring type, value representation, value type, read/write status, source category, and redaction status.
Scope: layer-1+

## FR-PROP-003 A connected client can edit approved public settable properties.

A connected client can edit approved public settable properties.
Scope: layer-1+

## FR-PROP-004 Failed property edits produce a clear, sanitized failure reason.

Failed property edits produce a clear, sanitized failure reason.
Scope: layer-1+

## FR-PROP-005 Blocked or redacted properties are visible as blocked/redacted, not silently omitted when metadata can safely be shown.

Blocked or redacted properties are visible as blocked/redacted, not silently omitted when metadata can safely be shown.
Scope: layer-1+

## FR-SEC-001 The app developer must explicitly enable the remote-control server.

The app developer must explicitly enable the remote-control server.
Scope: layer-1+

## FR-SEC-002 The app developer can disable remote control for production builds.

The app developer can disable remote control for production builds.
Scope: layer-1+

## FR-SEC-003 The client must authenticate before viewing tree, logs, or invoking commands.

The client must authenticate before viewing tree, logs, or invoking commands.
Scope: layer-1+

## FR-SEC-004 The client can connect through an ADB tunnel while still providing credentials.

The client can connect through an ADB tunnel while still providing credentials.
Scope: layer-1+

## FR-SEC-005 The user can see whether the session is loopback, ADB tunnel, or LAN/TLS.

The user can see whether the session is loopback, ADB tunnel, or LAN/TLS.
Scope: layer-1+

## FR-SEC-006 Rejected auth, blocked property access, and failed mutation attempts are visible in sanitized logs.

Rejected auth, blocked property access, and failed mutation attempts are visible in sanitized logs.
Scope: layer-1+

## FR-SEC-007 Remote actions and property changes are auditable.

Remote actions and property changes are auditable.
Scope: layer-1+

## FR-SEC-008 Sensitive control properties and log fields are redacted by default.

Sensitive control properties and log fields are redacted by default.
Scope: layer-1+

## FR-SEC-009 The client can forget saved endpoint/token/certificate settings.

The client can forget saved endpoint/token/certificate settings.
Scope: layer-1+

## FR-SEC-010 Inspect and accept TLS server certificate

The client can inspect a TLS server certificate fingerprint and explicitly accept it for a later connection.
Scope: layer-1+

## FR-SEC-011 Explicit live rendering opt-in

Live pixel streaming must require an explicit debuggee opt-in and remain disabled by default.
Scope: layer-1+

## FR-SEC-012 Explicit remote input opt-in

Live remote input must require an explicit debuggee opt-in in addition to existing remote action enablement.
Scope: layer-1+

## FR-TREE-001 A connected client can request the current Avalonia tree from the debuggee.

A connected client can request the current Avalonia tree from the debuggee.
Scope: layer-1+

## FR-TREE-002 Tree output includes stable node IDs, parent/child relationships, control type, name, automation ID, classes, bounds, visibility, enabled state, focus state, and common state metadata.

Tree output includes stable node IDs, parent/child relationships, control type, name, automation ID, classes, bounds, visibility, enabled state, focus state, and common state metadata.
Scope: layer-1+

## FR-TREE-003 Tree output identifies whether nodes come from visual tree, logical tree, top-level windows, popups, or flyouts when that distinction is available.

Tree output identifies whether nodes come from visual tree, logical tree, top-level windows, popups, or flyouts when that distinction is available.
Scope: layer-1+

## FR-TREE-004 A connected client can receive live tree/state updates.

A connected client can receive live tree/state updates.
Scope: layer-1+

## FR-TREE-005 The client preserves selection across updates when a node can be matched by stable ID or fallback path.

The client preserves selection across updates when a node can be matched by stable ID or fallback path.
Scope: layer-1+

