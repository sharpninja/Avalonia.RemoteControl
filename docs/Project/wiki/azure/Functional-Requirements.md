# Functional Requirements (MCP Server)

## FR-ACTION-001 A connected client can invoke a basic click on an approved clickable target.

A connected client can invoke a basic click on an approved clickable target.

## FR-ACTION-002 A connected client can request focus for a focusable target.

A connected client can request focus for a focusable target.

## FR-ACTION-003 Stale node IDs produce a recoverable client error and trigger a refresh path.

Stale node IDs produce a recoverable client error and trigger a refresh path.

## FR-ACTION-004 Unsupported actions are reported as unsupported with a reason.

Unsupported actions are reported as unsupported with a reason.

## FR-ACTION-005 Remote pointer and keyboard input

A connected client can send approved pointer, wheel, keyboard, and text input to the remote application from the live view.

## FR-ADB-001 The client can discover connected Android emulators/devices through `adb`.

The client can discover connected Android emulators/devices through `adb`.

## FR-ADB-002 The client can connect to an Avalonia app running on Android without the user manually typing port-forward commands.

The client can connect to an Avalonia app running on Android without the user manually typing port-forward commands.

## FR-ADB-003 The client supports selecting a specific device/emulator when multiple ADB targets are connected.

The client supports selecting a specific device/emulator when multiple ADB targets are connected.

## FR-ADB-004 The client can connect by package name, explicit endpoint, or discovered debug marker.

The client can connect by package name, explicit endpoint, or discovered debug marker.

## FR-ADB-005 The client tears down ADB forwarding when the session ends unless the user asks to keep it.

The client tears down ADB forwarding when the session ends unless the user asks to keep it.

## FR-ADB-006 The client exposes equivalent ADB workflows through CLI commands.

The client exposes equivalent ADB workflows through CLI commands.

## FR-ADB-007 Integrated desktop ADB connection workflow

The desktop client provides an integrated ADB connection workflow so users can list devices, launch a selected Android package when needed, create the ADB forward, save the transport-aware profile, and connect without running an external script.

## FR-CLIENT-001 The client provides Local, Network, and ADB connection modes.

The client provides Local, Network, and ADB connection modes.

## FR-CLIENT-002 The client renders the remote tree and selected-node details.

The client renders the remote tree and selected-node details.

## FR-CLIENT-003 The client provides a property inspector/editor for selected nodes.

The client provides a property inspector/editor for selected nodes.

## FR-CLIENT-004 The client provides action controls for supported remote interactions.

The client provides action controls for supported remote interactions.

## FR-CLIENT-005 The client provides a log viewer.

The client provides a log viewer.

## FR-CLIENT-006 The client shows connection state, authentication state, and transport mode.

The client shows connection state, authentication state, and transport mode.

## FR-CLIENT-008 Separate live remote view window

The desktop client must open a separate live remote UI window that can render the remote application outside the tree/property inspector.

## FR-CLIENT-009 Continuous live remote rendering

The live remote view must respond to changes in the remote UI without manual refresh.

## FR-CLIENT-010 Live view renders top-level visual surface

The live remote view must render the same top-level visual surface a user sees on the target device, including application background, popup, flyout, and overlay layers when Avalonia exposes them.

## FR-CLIENT-011 Live view click selects control tree node

When a user clicks a rendered control in the live remote UI window, the desktop client selects the corresponding node in the main control tree when that node is present in the current tree model.

## FR-CLIENT-012 Docked live view

The desktop client allows the live remote UI view to be docked on the right side of the main client window while preserving the existing separate live-view window option.

## FR-CLIENT-013 Visual Studio-style dockable panel chrome

The desktop client must provide Visual Studio-style tool-window chrome with visible icon commands for dock, float, auto-hide or pin, and close or hide actions; docked panels, pop-out windows, and dock commands must clearly separate draggable headers, content regions, active docked surfaces, and docking commands.

## FR-CLIENT-014 Project-scoped app connection settings

The desktop client stores connection settings by project and app so users can reconnect to local, network, or ADB debug targets without re-entering transport-specific settings.

## FR-CLIENT-015 Session log history per project

Each project records connection sessions with metadata and log history so previous debugging sessions can be reviewed after disconnecting.

## FR-CLIENT-016 Interaction recording for reproducible sessions

The client records remote-control interactions in a replayable session journal, including commands, timing, target context, and before/after state references needed to reproduce the debugging flow.

## FR-CLIENT-017 Replay and diff recorded sessions

The client can replay recorded interactions against a connected app and produce a per-step diff showing how the replayed app state differs from the original captured state.

## FR-CLIENT-018 Undock docked live view

When the live view is docked in the main client, the control command is labeled Undock and moves the live view back into a separate live-view window instead of simply closing the live-view surface.

## FR-CLIENT-019 Visual Studio 2026 client shell styling

The desktop client uses Visual Studio 2026-like dark shell styling across command bars, status bar, tool windows, tabs, dock headers, buttons, text inputs, lists, and pop-out windows.

## FR-CLIENT-020 Persistent client layout state

The desktop client remembers layout state such as window size, split sizes, selected right-side tab, log pop-out ownership, and docked live-view preference and restores that state on startup.

## FR-CLIENT-021 Interactive Visual Studio docking panels

Dockable client panels must expose interactive Visual Studio-like behavior, including draggable headers, icon-based panel management, floating pop-out windows, docking back into the shell, close or hide commands, persisted dock state, and clear user feedback for drag/drop dock targets.

## FR-CLIENT-022 Embedded terminal panel

The desktop client must host an embedded terminal panel that can launch Codex or another configured command inside the remote-control shell.

## FR-CLIENT-023 Terminal process lifecycle controls

The terminal panel must let users launch, interact with, and stop the configured CLI process without blocking the client UI or losing dock-panel behavior.

## FR-LOG-001 A connected client can stream `ILogger` events from the debuggee.

A connected client can stream `ILogger` events from the debuggee.

## FR-LOG-002 Log output includes timestamp, level, category, event ID, message, structured state summary, scope summary, exception summary, sequence number, and dropped-message count.

Log output includes timestamp, level, category, event ID, message, structured state summary, scope summary, exception summary, sequence number, and dropped-message count.

## FR-LOG-003 The client can filter logs by level and category.

The client can filter logs by level and category.

## FR-LOG-004 The client shows when logs were dropped because of buffering/backpressure.

The client shows when logs were dropped because of buffering/backpressure.

## FR-LOG-005 Client log verbosity setting

The client exposes a log verbosity setting with Debug, Information, Warning, and Error options and uses the selected level when streaming ILogger events.

## FR-LOG-006 Debug protocol event logging

Every remote-control operation sent from the client and every non-logstream response or stream update sent to the client emits a sanitized Debug ILogger message.

## FR-LOG-007 Client log stream visibility

The desktop client starts log streaming after connection by default, shows current log stream state and entry count, and displays log-stream failures inline instead of only in a transient global status message.

## FR-LOG-008 Pop-out log viewer

The desktop client allows the current log stream to be opened in a separate log window while keeping the main client connected and streaming.

## FR-LOG-009 Undocked log panel

When the desktop client opens logs in a pop-out window, the main window must remove the embedded log list and expose a clear way to dock the popped-out logs back into the main window.

## FR-LOG-010 Pop-out log verbosity control

The pop-out log window exposes the same Debug, Information, Warning, and Error verbosity selector as the main log panel and changing either selector updates the active log stream setting.

## FR-PROP-001 A connected client can inspect safe readable public CLR properties and Avalonia properties for a selected node.

A connected client can inspect safe readable public CLR properties and Avalonia properties for a selected node.

## FR-PROP-002 Property output identifies property name, declaring type, value representation, value type, read/write status, source category, and redaction status.

Property output identifies property name, declaring type, value representation, value type, read/write status, source category, and redaction status.

## FR-PROP-003 A connected client can edit approved public settable properties.

A connected client can edit approved public settable properties.

## FR-PROP-004 Failed property edits produce a clear, sanitized failure reason.

Failed property edits produce a clear, sanitized failure reason.

## FR-PROP-005 Blocked or redacted properties are visible as blocked/redacted, not silently omitted when metadata can safely be shown.

Blocked or redacted properties are visible as blocked/redacted, not silently omitted when metadata can safely be shown.

## FR-SEC-001 The app developer must explicitly enable the remote-control server.

The app developer must explicitly enable the remote-control server.

## FR-SEC-002 The app developer can disable remote control for production builds.

The app developer can disable remote control for production builds.

## FR-SEC-003 The client must authenticate before viewing tree, logs, or invoking commands.

The client must authenticate before viewing tree, logs, or invoking commands.

## FR-SEC-004 The client can connect through an ADB tunnel while still providing credentials.

The client can connect through an ADB tunnel while still providing credentials.

## FR-SEC-005 The user can see whether the session is loopback, ADB tunnel, or LAN/TLS.

The user can see whether the session is loopback, ADB tunnel, or LAN/TLS.

## FR-SEC-006 Rejected auth, blocked property access, and failed mutation attempts are visible in sanitized logs.

Rejected auth, blocked property access, and failed mutation attempts are visible in sanitized logs.

## FR-SEC-007 Remote actions and property changes are auditable.

Remote actions and property changes are auditable.

## FR-SEC-008 Sensitive control properties and log fields are redacted by default.

Sensitive control properties and log fields are redacted by default.

## FR-SEC-009 The client can forget saved endpoint/token/certificate settings.

The client can forget saved endpoint/token/certificate settings.

## FR-SEC-010 Inspect and accept TLS server certificate

The client can inspect a TLS server certificate fingerprint and explicitly accept it for a later connection.

## FR-SEC-011 Explicit live rendering opt-in

Live pixel streaming must require an explicit debuggee opt-in and remain disabled by default.

## FR-SEC-012 Explicit remote input opt-in

Live remote input must require an explicit debuggee opt-in in addition to existing remote action enablement.

## FR-TREE-001 A connected client can request the current Avalonia tree from the debuggee.

A connected client can request the current Avalonia tree from the debuggee.

## FR-TREE-002 Tree output includes stable node IDs, parent/child relationships, control type, name, automation ID, classes, bounds, visibility, enabled state, focus state, and common state metadata.

Tree output includes stable node IDs, parent/child relationships, control type, name, automation ID, classes, bounds, visibility, enabled state, focus state, and common state metadata.

## FR-TREE-003 Tree output identifies whether nodes come from visual tree, logical tree, top-level windows, popups, or flyouts when that distinction is available.

Tree output identifies whether nodes come from visual tree, logical tree, top-level windows, popups, or flyouts when that distinction is available.

## FR-TREE-004 A connected client can receive live tree/state updates.

A connected client can receive live tree/state updates.

## FR-TREE-005 The client preserves selection across updates when a node can be matched by stable ID or fallback path.

The client preserves selection across updates when a node can be matched by stable ID or fallback path.

