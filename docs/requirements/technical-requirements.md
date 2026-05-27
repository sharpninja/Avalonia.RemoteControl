# Technical Requirements

## Platform and Packaging

- `TR-PLAT-CORE-001`: Target .NET 10.
- `TR-PLAT-CORE-002`: Target Avalonia 12.
- `TR-PACK-PACKAGE-001`: Package the server SDK as `SharpNinja.Avalonia.RemoteControl.Server`.
- `TR-PACK-PACKAGE-002`: Package the client launcher as `SharpNinja.Avalonia.RemoteControl.Tool`.
- `TR-PACK-PACKAGE-003`: The .NET tool command name is `avalonia-remote`.
- `TR-PACK-PACKAGE-004`: Packages include symbols and SourceLink when package infrastructure is implemented.
- `TR-PACK-PACKAGE-005`: Package the host-independent runtime as `SharpNinja.Avalonia.RemoteControl.Runtime` for Android-compatible consumers.
- `TR-PACK-PACKAGE-006`: Package the shared protocol contracts as `SharpNinja.Avalonia.RemoteControl.Protocol` so SDK package dependencies resolve from NuGet.

## Protocol

- `TR-GRPC-PROTOCOL-001`: Define a versioned protobuf contract for desktop-facing communication.
- `TR-GRPC-PROTOCOL-002`: Provide `GetCapabilities`.
- `TR-GRPC-PROTOCOL-003`: Provide `GetSnapshot`.
- `TR-GRPC-PROTOCOL-004`: Provide `WatchTree`.
- `TR-GRPC-PROTOCOL-005`: Provide `InvokeClick`.
- `TR-GRPC-PROTOCOL-006`: Provide `SetProperty`.
- `TR-GRPC-PROTOCOL-007`: Provide `WatchLogs`.
- `TR-GRPC-PROTOCOL-008`: Streaming responses include sequence/version data sufficient for reconnect and resync behavior.
- `TR-GRPC-PROTOCOL-009`: Provide `WatchFrames` as an additive stream carrying PNG frame bytes, pixel size, root DIP size, render scale, sequence, and timestamp metadata.
- `TR-GRPC-PROTOCOL-010`: Provide `SendInput` as an additive unary operation accepting batched pointer, wheel, key, and text events in root-relative DIP coordinates.

## Dependency Injection and Hosting

- `TR-DI-HOSTING-001`: Expose `IServiceCollection` integration for server registration.
- `TR-DI-HOSTING-002`: Expose `IServiceProvider` startup integration.
- `TR-DI-HOSTING-003`: Expose `AvaloniaRemoteControlOptions`.
- `TR-DI-HOSTING-004`: Integrate with application lifetime so the server starts and stops cleanly.
- `TR-DI-HOSTING-005`: Do not replace or suppress existing application logging providers.

## Avalonia Runtime Access

- `TR-UI-RUNTIME-001`: All Avalonia tree, property, and action access runs through the Avalonia UI dispatcher.
- `TR-UI-RUNTIME-002`: Snapshot capture handles top-level windows.
- `TR-UI-RUNTIME-003`: Snapshot capture handles popups/flyouts when Avalonia exposes them safely.
- `TR-UI-RUNTIME-004`: Virtualized or unrealized items are represented as unavailable rather than fabricated.
- `TR-UI-RUNTIME-005`: Stale node IDs are detected and reported.
- `TR-UI-RUNTIME-006`: Tree snapshots expose root-relative absolute bounds while preserving existing local bounds values.
- `TR-UI-RUNTIME-007`: Frame capture runs on the Avalonia UI dispatcher using `RenderTargetBitmap`, enforces max frame size, and supports cancellation-aware periodic streaming at a default cadence of 10 FPS.
- `TR-UI-RUNTIME-008`: Runtime frame capture, live tree snapshots, and live input dispatch normalize an app-provided `Control` root to its containing Avalonia `TopLevel` before rendering, traversing, or dispatching input.

## Client UI

- `TR-CLIENT-UI-001`: The desktop tool defines reusable Avalonia styles and resources for dockable tool windows, tool-window headers, command bars, dock placeholders, and live-view/log surfaces so the main window and generic floating tool windows use consistent Visual Studio-like panel chrome.
- `TR-CLIENT-UI-002`: The desktop tool defines reusable Visual Studio 2026-like Avalonia resources and styles for the entire shell, including command bars, status bars, text inputs, combo boxes, buttons, list/tree surfaces, tab strips, tool-window headers, dock placeholders, and generic floating tool windows.
- `TR-CLIENT-UI-003`: The desktop tool implements a reusable dock chrome surface for panels and floating windows that renders Visual Studio-like command icons, tooltips, draggable headers, drag state, dock, float, auto-hide, and close commands, and routes those commands through explicit panel identifiers.
- `TR-CLIENT-UI-004`: Every displayed dock panel is a custom Avalonia control backed by a view model; the main window composes panel hosts and coordinates cross-panel session actions rather than owning each panel's internal UI.

## Client Project System

- `TR-CLIENT-PROJECT-001`: The client project system persists projects as versioned JSON in user-scoped storage, with stable project, app profile, session, log, interaction, and artifact identifiers.
- `TR-CLIENT-PROJECT-002`: The project store captures per-session metadata, sanitized log rows, connection profile references, and bounded retention metadata without duplicating active streaming state.
- `TR-CLIENT-LAYOUT-001`: The project document persists a client layout state object with window bounds, splitter dimensions, right-side selected tab, log panel floating state, live-view dock state, and dock-pane auto-hide state; the main window captures state before save/close and applies valid saved state after project load.
- `TR-CLIENT-REPLAY-001`: The client records replay steps with command type, order, timing, target node, connection context, before/after tree artifact references, and replay results; replay diffs compare original and replayed tree snapshots per step.
- `TR-CLIENT-REPLAY-002`: Interaction replay artifacts are user-scoped project data; bearer tokens, certificates, and typed text must not be written to `ILogger` diagnostics, and replay records must identify sensitive payload fields.

## Property Mutation

- `TR-PROP-MUTATION-001`: Property mutation is deny-by-default unless allowed by configured policy.
- `TR-PROP-MUTATION-002`: Supported scalar conversions include string, bool, numeric types, enum, nullable scalar, `Thickness`, `CornerRadius`, `Point`, `Size`, `Rect`, and common color/brush representations when feasible.
- `TR-PROP-MUTATION-003`: Unsupported property types are reported as unsupported.
- `TR-PROP-MUTATION-004`: Indexers, delegates, arbitrary object graphs, services, collections, and private members are blocked in v1.
- `TR-PROP-MUTATION-005`: Failed validation or conversion returns a sanitized error summary.

## Actions

- `TR-ACTION-INVOCATION-001`: Click invocation runs on the Avalonia UI dispatcher.
- `TR-ACTION-INVOCATION-002`: Click invocation uses the visible center of the selected node by default.
- `TR-ACTION-INVOCATION-003`: Command-control semantic invocation may be used when pointer event synthesis is not appropriate.
- `TR-ACTION-INVOCATION-004`: Unsupported drag/drop and arbitrary method invocation are out of v1 unless added through future requirements.
- `TR-ACTION-INVOCATION-005`: Live remote input dispatches pointer, wheel, keyboard, and text events through the Avalonia UI dispatcher, maintains pointer state for drag sequences, and targets keyboard/text input to the focused element.

## Client Live View

- `TR-LIVE-VIEW-011`: The live remote UI window maps pointer-click coordinates to root-relative DIPs, hit-tests the latest visible tree nodes using absolute bounds from deepest/topmost node to root, raises the selected node ID to the main window, and the main window selects and reveals the matching control-tree item when present.
- `TR-LIVE-VIEW-012`: The desktop client factors the live-view rendering and input surface into a reusable control that can be hosted either in a generic floating tool window or in a right-side dock area of the main window, with only one stream per hosted live-view instance and the same node-selection callback behavior.
- `TR-LIVE-VIEW-013`: The docked live-view float command moves the hosted live-view experience into a generic `FloatingDockPaneWindow` with the same session, capability, selection, and input recording callbacks.

## Logging

- `TR-LOG-STREAMING-001`: Implement a bounded `ILoggerProvider`.
- `TR-LOG-STREAMING-002`: Log streaming captures timestamp, level, category, event ID, rendered message, structured state summary, scope summary, exception summary, sequence number, and dropped count.
- `TR-LOG-STREAMING-003`: Buffer limits are configurable.
- `TR-LOG-STREAMING-004`: Dropped messages are counted and surfaced to clients.
- `TR-LOG-STREAMING-005`: Log streaming applies sensitive-data redaction.
- `TR-LOG-STREAMING-006`: The desktop client maps log verbosity selections to `Microsoft.Extensions.Logging.LogLevel` names and sends the selected minimum level in `WatchLogs` requests.
- `TR-LOG-STREAMING-007`: The shared runtime emits Debug `ILogger` messages for client request receipt, unary response completion, live tree update sends, live frame sends, log-stream lifecycle, and remote input command completion without logging bearer tokens, property values, or typed text.
- `TR-LOG-STREAMING-008`: The Android bridge TCP transport emits Debug `ILogger` diagnostics for accepted client sockets, decoded request frames, sent response frames, and stream completion without logging bearer tokens or payload contents; `WatchLogs` streams log only lifecycle and completion diagnostics to avoid recursive log generation.
- `TR-LOG-STREAMING-009`: The remote-control `ILoggerProvider` is registered with a provider-specific filter that captures Debug and higher log entries for streaming without lowering or replacing filters for existing application logging providers.
- `TR-LOG-STREAMING-010`: The desktop client defaults log verbosity to Warning, starts `WatchLogs` automatically after a successful connection, restarts the stream when verbosity changes, shows active/stopped/error state and entry counts near the log list, and formats rows with sequence, timestamp, level, category, event ID, message, exception summary, structured state, scope, and dropped count when present.
- `TR-LOG-STREAMING-011`: The desktop client exposes a floating log tool panel backed by the same live observable log collection as the main log panel, preserving active stream state, verbosity behavior, entry counts, and sanitized error display without starting a duplicate `WatchLogs` stream.
- `TR-LOG-STREAMING-012`: The desktop client tracks whether the shared log view is embedded or floating, hides the embedded log list while floating, and restores it when the generic tool window docks or closes without starting a duplicate `WatchLogs` stream.
- `TR-LOG-STREAMING-013`: The desktop client stores selected log verbosity in shared log view state so embedded and floating log panels expose synchronized Debug, Information, Warning, and Error selections and stream restarts use the shared minimum level.

## Android ADB Connectivity

- `TR-ADB-CONNECTIVITY-001`: Add an ADB connection profile to `avalonia-remote`.
- `TR-ADB-CONNECTIVITY-002`: Use `adb forward tcp:<hostPort> tcp:<devicePort>` for host-client-to-device-app connections.
- `TR-ADB-CONNECTIVITY-003`: Support `adb -s <serial>` everywhere.
- `TR-ADB-CONNECTIVITY-004`: Store Android debug endpoint metadata in a debuggable-app-accessible marker or equivalent discovery mechanism.
- `TR-ADB-CONNECTIVITY-005`: Provide explicit fallback flags for package, port, token, and certificate mode.
- `TR-ADB-CONNECTIVITY-006`: Keep bearer authentication required over ADB tunnels.
- `TR-ADB-CONNECTIVITY-007`: Clean up ADB forwards by default when the client disconnects.
- `TR-ADB-CONNECTIVITY-008`: Technical Spike 0 must prove the chosen Android app-side transport before implementation depends on it.
- `TR-ADB-CONNECTIVITY-009`: Android app-side hosting must not depend on `Microsoft.AspNetCore.App` because .NET Android does not provide an Android runtime pack for that framework reference.
- `TR-ADB-CONNECTIVITY-010`: Android bridge proof must include build, install, launch, package marker read, ADB forward, authenticated capability probe, tree snapshot capture, and forward cleanup evidence on a real emulator or device.
- `TR-ADB-CONNECTIVITY-011`: Android package-private markers must include versioned transport protocol metadata; missing protocol metadata is interpreted as legacy `grpc` for backward compatibility.
- `TR-ADB-CONNECTIVITY-012`: The client must fail closed before creating an ADB forward when a package marker advertises a transport protocol the client does not implement.
- `TR-ADB-CONNECTIVITY-013`: Android-compatible runtime services must be isolated from ASP.NET Core/Kestrel host dependencies before Android bridge app-side implementation starts.
- `TR-ADB-CONNECTIVITY-014`: The Android bridge protocol must use a versioned length-prefixed protobuf envelope that carries bearer authentication, request identity, method identity, payload bytes, response status, and sanitized error details.
- `TR-ADB-CONNECTIVITY-015`: The Android app-side bridge listener must bind to loopback, handle authenticated unary bridge requests, expose package-private marker metadata, and stop cleanly with the debuggee app lifecycle.
- `TR-ADB-CONNECTIVITY-016`: A successful `avalonia-remote adb connect --keep-forward` session must save a user-scoped connection profile containing endpoint, token, and transport protocol so the desktop UI can attach to the kept forward using the marker-advertised transport.
- `TR-ADB-CONNECTIVITY-017`: The Android bridge transport supports long-lived streaming responses for `WatchTree` and `WatchFrames` and ends streams on client cancellation or socket close.
- `TR-ADB-CONNECTIVITY-018`: Package-marker ADB connect must detect a stopped Android package before forwarding, and bridge client probes must convert early closed bridge sockets into sanitized user-facing diagnostics instead of raw transport exceptions.
- `TR-ADB-CONNECTIVITY-019`: The desktop client uses the same ADB discovery, package launch, marker read, `adb forward`, authenticated probe, profile save, and cleanup services as the CLI workflow; it defaults to keeping the forward active for the current desktop session and connects immediately after a successful probe.
- `TR-ADB-CONNECTIVITY-020`: When the desktop client is configured for `arc-protobuf-v1`, a loopback endpoint, and a selected ADB device, the top Connect action creates or refreshes the ADB forward before probing the endpoint so users do not need an external script or separate Android Connect flow for explicit device-port sessions.

## Security Constraints

- `TR-SEC-SECURITY-001`: Server is disabled by default and requires explicit startup/configuration.
- `TR-SEC-SECURITY-002`: Bearer authentication is required on every RPC, including loopback and ADB tunnel sessions.
- `TR-SEC-SECURITY-003`: Tokens are configurable, rotatable, and never written to logs, exceptions, traces, or package artifacts.
- `TR-SEC-SECURITY-004`: TLS is required for all non-loopback network listeners.
- `TR-SEC-SECURITY-005`: Cleartext h2c is allowed only for loopback or explicitly detected ADB-forwarded localhost sessions.
- `TR-SEC-SECURITY-006`: Default listener binds to loopback only.
- `TR-SEC-SECURITY-007`: LAN binding requires explicit endpoint configuration and TLS certificate configuration.
- `TR-SEC-SECURITY-008`: Property exposure applies default redaction for sensitive names such as password, token, secret, key, credential, auth, cookie, and connection string.
- `TR-SEC-SECURITY-009`: Log streaming applies the same sensitive-data redaction policy.
- `TR-SEC-SECURITY-010`: Property mutation is deny-by-default unless allowed by configured policy.
- `TR-SEC-SECURITY-011`: All mutation commands pass through a command authorization policy.
- `TR-SEC-SECURITY-012`: Mutation and action failures return safe error summaries instead of raw exception dumps.
- `TR-SEC-SECURITY-013`: Every remote mutation/action emits an audit log with timestamp, authenticated client identity, node ID, command type, result, and sanitized details.
- `TR-SEC-SECURITY-014`: Rejected authentication, rejected authorization, blocked property access, and failed mutation attempts emit audit logs.
- `TR-SEC-SECURITY-015`: ADB forwarding cleanup runs by default when the client disconnects.
- `TR-SEC-SECURITY-016`: Client persists connection settings only in user-scoped storage and never logs tokens.
- `TR-SEC-SECURITY-017`: Manually accepted TLS certificates are persisted as SHA-256 certificate fingerprints and connections succeed only when the presented server certificate matches the accepted fingerprint or configured certificate file.
- `TR-SEC-SECURITY-018`: Live frame streaming is disabled by default and rejected unless `AllowRemoteFrames` is enabled.
- `TR-SEC-SECURITY-019`: Live remote input is disabled by default, requires `AllowRemoteActions` and `AllowRemoteInput`, and emits sanitized audit logs without recording typed text.

## CI and Release

- `TR-CI-RELEASE-001`: GitHub Actions restores, builds, tests, packs, and uploads artifacts.
- `TR-CI-RELEASE-002`: Azure Pipelines restores, builds, tests, packs, and uploads artifacts.
- `TR-CI-RELEASE-003`: Tagged `v*` releases publish packages through protected secrets/service connections.
- `TR-CI-RELEASE-004`: Duplicate publish prevention exists across GitHub and Azure release paths.
- `TR-CI-RELEASE-005`: Public GitHub and private Azure DevOps source-of-truth policy is documented before first package publish.

## Documentation

- `TR-DOC-USER-001`: User documentation covers installation, server integration, client operation, Android ADB connection, security posture, and troubleshooting for published packages.
