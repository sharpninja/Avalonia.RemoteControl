# Technical Requirements (MCP Server)

## TR-ACTION-INVOCATION-001

**Click invocation runs on the Avalonia UI dispatcher.** — Click invocation runs on the Avalonia UI dispatcher.

## TR-ACTION-INVOCATION-002

**Click invocation uses the visible center of the selected node by default.** — Click invocation uses the visible center of the selected node by default.

## TR-ACTION-INVOCATION-003

**Command-control semantic invocation may be used when pointer event synthesis is not appropriate.** — Command-control semantic invocation may be used when pointer event synthesis is not appropriate.

## TR-ACTION-INVOCATION-004

**Unsupported gestures, text input, drag/drop, and arbitrary method invocation are out of v1 unless added through future requirements.** — Unsupported gestures, text input, drag/drop, and arbitrary method invocation are out of v1 unless added through future requirements.

## TR-ACTION-INVOCATION-005

**Dispatcher-safe live input dispatch** — Remote live input must dispatch pointer, wheel, keyboard, and text events through the Avalonia UI dispatcher and maintain pointer state for drag sequences.

## TR-ADB-CONNECTIVITY-001

**Add an ADB connection profile to `avalonia-remote`.** — Add an ADB connection profile to `avalonia-remote`.

## TR-ADB-CONNECTIVITY-002

**Use `adb forward tcp:<hostPort> tcp:<devicePort>` for host-client-to-device-app connections.** — Use `adb forward tcp:<hostPort> tcp:<devicePort>` for host-client-to-device-app connections.

## TR-ADB-CONNECTIVITY-003

**Support `adb -s <serial>` everywhere.** — Support `adb -s <serial>` everywhere.

## TR-ADB-CONNECTIVITY-004

**Store Android debug endpoint metadata in a debuggable-app-accessible marker or equivalent discovery mechanism.** — Store Android debug endpoint metadata in a debuggable-app-accessible marker or equivalent discovery mechanism.

## TR-ADB-CONNECTIVITY-005

**Provide explicit fallback flags for package, port, token, and certificate mode.** — Provide explicit fallback flags for package, port, token, and certificate mode.

## TR-ADB-CONNECTIVITY-006

**Keep bearer authentication required over ADB tunnels.** — Keep bearer authentication required over ADB tunnels.

## TR-ADB-CONNECTIVITY-007

**Clean up ADB forwards by default when the client disconnects.** — Clean up ADB forwards by default when the client disconnects.

## TR-ADB-CONNECTIVITY-008

**Technical Spike 0 must prove the chosen Android app-side transport before implementation depends on it.** — Technical Spike 0 must prove the chosen Android app-side transport before implementation depends on it.

## TR-ADB-CONNECTIVITY-009

**Android app-side transport avoids Microsoft.AspNetCore.App** — Android app-side transport must not depend on Microsoft.AspNetCore.App, ASP.NET Core hosting, or Kestrel runtime packs that are unavailable for net10.0-android; use an Android-compatible bridge or adapter while preserving the remote-control capability contract.

## TR-ADB-CONNECTIVITY-010

**Android bridge proof evidence** — Android bridge proof must include build, install, launch, package marker read, ADB forward, authenticated capability probe, tree snapshot capture, and forward cleanup evidence on a real emulator or device.

## TR-ADB-CONNECTIVITY-011

**Versioned Android marker transport metadata** — Android package-private markers must include versioned transport protocol metadata; missing protocol metadata is interpreted as legacy grpc for backward compatibility.

## TR-ADB-CONNECTIVITY-012

**Fail closed for unsupported Android marker protocols** — The client must fail closed before creating an ADB forward when a package marker advertises a transport protocol the client does not implement.

## TR-ADB-CONNECTIVITY-013

**Android runtime services isolated from ASP.NET Core host dependencies** — Android-compatible runtime services must be isolated from ASP.NET Core and Kestrel host dependencies before Android bridge app-side implementation starts.

## TR-ADB-CONNECTIVITY-014

**Android bridge protobuf envelope** — The Android bridge protocol must use a versioned length-prefixed protobuf envelope that carries bearer authentication, request identity, method identity, payload bytes, response status, and sanitized error details.

## TR-ADB-CONNECTIVITY-015

**Android app-side bridge listener lifecycle** — The Android app-side bridge listener must bind to loopback, handle authenticated unary bridge requests, expose package-private marker metadata, and stop cleanly with the debuggee app lifecycle.

## TR-ADB-CONNECTIVITY-017

**Android bridge streaming support** — The Android bridge transport must support long-lived streaming responses for WatchTree and WatchFrames and end streams on client cancellation or socket close.

## TR-ADB-CONNECTIVITY-018

**ADB bridge probe reports stopped package or closed bridge cleanly** — The ADB package-marker connect flow must detect when the Android package is not running before creating a forward, and bridge client probes must convert an early closed bridge socket into a sanitized user-facing diagnostic instead of surfacing raw transport exceptions.

## TR-ADB-CONNECTIVITY-019

**Desktop ADB workflow reuses CLI services** — The desktop client uses the same ADB discovery, package launch, marker read, adb forward, authenticated probe, profile save, and cleanup services as the CLI workflow; it defaults to keeping the forward active for the current desktop session and connects immediately after a successful probe.

## TR-ADB-CONNECTIVITY-020

**Desktop Connect prepares selected ADB bridge forward** — When the desktop client is configured for arc-protobuf-v1, a loopback endpoint, and a selected ADB device, the top Connect action creates or refreshes the ADB forward before probing the endpoint so users do not need an external script or separate Android Connect flow for explicit device-port sessions.

## TR-ANDROID-ADB-001

**ADB command service abstraction** — Android MCP tools must execute ADB through the existing ProcessAdbCommandRunner/AdbClient style abstractions, support explicit serial selection, avoid shell injection, redact sensitive values from logs, and keep command timeouts bounded.

## TR-ANDROID-AVD-001

**AVD manager and emulator launch support** — The client must discover Android SDK emulator and avdmanager executables, list AVD names, and launch a selected AVD such as Pixel 6 without blocking the client UI or MCP server.

## TR-ANDROID-LOG-001

**Log polling keepalive and recovery** — Android-backed client connections must maintain or recover log polling for remote-control endpoints so idle log-stream timeouts do not invalidate the active debugging session without a reconnect path.

## TR-ANDROID-MCP-001

**Android MCP tool catalog** — Additive MCP tools must be registered in the embedded client MCP server with JSON-schema inputs, sanitized outputs, and deterministic tool names prefixed with avalonia_android_ for device, emulator, app, log, screenshot, UI tree, and input operations.

## TR-CI-RELEASE-001

**GitHub Actions restores, builds, tests, packs, and uploads artifacts.** — GitHub Actions restores, builds, tests, packs, and uploads artifacts.

## TR-CI-RELEASE-002

**Azure Pipelines restores, builds, tests, packs, and uploads artifacts.** — Azure Pipelines restores, builds, tests, packs, and uploads artifacts.

## TR-CI-RELEASE-003

**Tagged `v*` releases publish packages through protected secrets/service connections.** — Tagged `v*` releases publish packages through protected secrets/service connections.

## TR-CI-RELEASE-004

**Duplicate publish prevention exists across GitHub and Azure release paths.** — Duplicate publish prevention exists across GitHub and Azure release paths.

## TR-CI-RELEASE-005

**Public GitHub and private Azure DevOps source-of-truth policy is documented before first package publish.** — Public GitHub and private Azure DevOps source-of-truth policy is documented before first package publish.

## TR-CLIENT-LAYOUT-001

**Client layout state persistence** — The project document persists a client layout state object with window bounds, splitter dimensions, right-side selected tab, log panel pop-out state, and live-view dock state; the main window captures state before save/close and applies valid saved state after project load.

## TR-CLIENT-LAYOUT-002

**Main shell dock layout regions** — The main client workspace must be composed with a dock layout that places the control tree in the west region, remote tools in the east region, logs in the south region, and the undeclared fill region as the default workspace surface.

## TR-CLIENT-MCP-001

**In-process loopback MCP transport** — The desktop application starts an in-process Streamable HTTP MCP endpoint by default on loopback, validates the request path and Origin header, serves JSON-RPC requests over HTTP POST, returns 405 for unsupported GET streams, and never requires Codex to launch avalonia-remote mcp as a child server process.

## TR-CLIENT-MCP-002

**Remote-control MCP tools** — The MCP host exposes approved tools for remote capabilities, tree snapshot, click, focus, and property mutation by adapting RemoteControlDesktopSession with the configured endpoint, token, transport protocol, and certificate trust settings.

## TR-CLIENT-MCP-003

**Self-contained Codex terminal registration** — The embedded terminal Codex launch profile registers the running app MCP Streamable HTTP URL through Codex mcp_servers configuration, passes only the in-process loopback URL plus a seed prompt, launches from the tool process startup working directory by default, documents the available remote-control tools, requires snapshot/tree-first node discovery, and avoids screenshots or pixel inspection as the primary control-selection path; it must not pass remote endpoint, transport, bearer token, profile, environment variable, or avalonia-remote child-process arguments.

## TR-CLIENT-PROJECT-001

**User-scoped project store** — The client project system persists projects as versioned JSON in user-scoped storage, with stable project, app profile, session, log, interaction, and artifact identifiers.

## TR-CLIENT-PROJECT-002

**Project sessions and log retention** — The project store captures per-session metadata, sanitized log rows, connection profile references, and bounded retention metadata without duplicating active streaming state.

## TR-CLIENT-REPLAY-001

**Replay journal and state diff model** — The client records replay steps with command type, order, timing, target node, connection context, before/after tree artifact references, and replay results; replay diffs compare original and replayed tree snapshots per step.

## TR-CLIENT-REPLAY-002

**Replay data sensitivity** — Interaction replay artifacts are user-scoped project data; bearer tokens, certificates, and typed text must not be written to ILogger diagnostics, and replay records must identify sensitive payload fields.

## TR-CLIENT-TERM-001

**Iciclecreek terminal integration** — The terminal panel must use Iciclecreek.Avalonia.Terminal for terminal rendering and route process launch, input, output, and disposal through explicit lifecycle code.

## TR-CLIENT-UI-001

**Tool-window style resources** — The desktop tool must define reusable Avalonia styles and resources for dockable tool windows, tool-window headers, command bars, dock placeholders, and live-view/log surfaces so the main window and pop-out windows use consistent Visual Studio-like panel chrome.

## TR-CLIENT-UI-002

**Visual Studio 2026 shell resources** — The desktop tool defines reusable Visual Studio 2026-like Avalonia resources and styles for the entire shell, including command bars, status bars, text inputs, combo boxes, buttons, list/tree surfaces, tab strips, tool-window headers, dock placeholders, and pop-out windows.

## TR-CLIENT-UI-003

**Interactive dock chrome control** — The desktop tool must implement a reusable dock chrome surface for panels and floating windows that renders Visual Studio-like command icons, tooltips, draggable headers, drag state, dock, float, auto-hide, and close commands, and routes those commands through explicit panel identifiers.

## TR-CLIENT-UI-004

**Panel controls backed by view models** — Every displayed dock panel must be a custom Avalonia control backed by a view model; the main window composes panel hosts and coordinates cross-panel session actions rather than owning each panel internal UI.

## TR-CLIENT-UI-005

**Terminal dock panel control** — The terminal surface must be a custom Avalonia control backed by a terminal view model and hosted through the same dock-panel composition model as the other client panels.

## TR-DI-HOSTING-001

**Expose `IServiceCollection` integration for server registration.** — Expose `IServiceCollection` integration for server registration.

## TR-DI-HOSTING-002

**Expose `IServiceProvider` startup integration.** — Expose `IServiceProvider` startup integration.

## TR-DI-HOSTING-003

**Expose `AvaloniaRemoteControlOptions`.** — Expose `AvaloniaRemoteControlOptions`.

## TR-DI-HOSTING-004

**Integrate with application lifetime so the server starts and stops cleanly.** — Integrate with application lifetime so the server starts and stops cleanly.

## TR-DI-HOSTING-005

**Do not replace or suppress existing application logging providers.** — Do not replace or suppress existing application logging providers.

## TR-DOC-USER-001

**User documentation coverage** — User documentation covers installation, server integration, client operation, local desktop quickstart, Android ADB desktop and CLI connection, embedded Codex MCP usage, security posture, settings, troubleshooting, and current published package version guidance.

## TR-GRPC-PROTOCOL-001

**Define a versioned protobuf contract for desktop-facing communication.** — Define a versioned protobuf contract for desktop-facing communication.

## TR-GRPC-PROTOCOL-002

**Provide `GetCapabilities`.** — Provide `GetCapabilities`.

## TR-GRPC-PROTOCOL-003

**Provide `GetSnapshot`.** — Provide `GetSnapshot`.

## TR-GRPC-PROTOCOL-004

**Provide `WatchTree`.** — Provide `WatchTree`.

## TR-GRPC-PROTOCOL-005

**Provide `InvokeClick`.** — Provide `InvokeClick`.

## TR-GRPC-PROTOCOL-006

**Provide `SetProperty`.** — Provide `SetProperty`.

## TR-GRPC-PROTOCOL-007

**Provide `WatchLogs`.** — Provide `WatchLogs`.

## TR-GRPC-PROTOCOL-008

**Streaming responses include sequence/version data sufficient for reconnect and resync behavior.** — Streaming responses include sequence/version data sufficient for reconnect and resync behavior.

## TR-GRPC-PROTOCOL-009

**Frame streaming protocol** — The protocol must define an additive WatchFrames stream carrying PNG frame bytes, pixel size, root DIP size, render scale, sequence, and timestamp metadata.

## TR-GRPC-PROTOCOL-010

**Remote input protocol** — The protocol must define an additive SendInput unary operation that accepts batched pointer, wheel, key, and text events in root-relative DIP coordinates.

## TR-LIVE-VIEW-011

**Live view hit testing selects main tree node** — The live remote UI window maps pointer-click coordinates to root-relative DIPs, hit-tests the latest visible tree nodes using absolute bounds from deepest/topmost node to root, raises the selected node ID to the main window, and the main window selects and reveals the matching control-tree item when present.

## TR-LIVE-VIEW-012

**Reusable live-view host** — The desktop client factors the live-view rendering and input surface into a reusable control that can be hosted either in a generic floating tool window or directly in the right-side Remote Tools tab area, with no nested live-view dock chrome, constrained or scrollable content, only one stream per hosted live-view instance, and the same node-selection callback behavior.

## TR-LIVE-VIEW-013

**Live view undock transfer** — The docked live-view command is labeled Undock and moves the hosted live-view experience back into a separate RemoteViewWindow with the same session, capability, selection, and input recording callbacks.

## TR-LOG-STREAMING-001

**Implement a bounded `ILoggerProvider`.** — Implement a bounded `ILoggerProvider`.

## TR-LOG-STREAMING-002

**Log streaming captures timestamp, level, category, event ID, rendered message, structured state summary, scope summary, exception summary, sequence number, and dropped count.** — Log streaming captures timestamp, level, category, event ID, rendered message, structured state summary, scope summary, exception summary, sequence number, and dropped count.

## TR-LOG-STREAMING-003

**Buffer limits are configurable.** — Buffer limits are configurable.

## TR-LOG-STREAMING-004

**Dropped messages are counted and surfaced to clients.** — Dropped messages are counted and surfaced to clients.

## TR-LOG-STREAMING-005

**Log streaming applies sensitive-data redaction.** — Log streaming applies sensitive-data redaction.

## TR-LOG-STREAMING-006

**WatchLogs uses selected verbosity** — The desktop client maps log verbosity selections to Microsoft.Extensions.Logging LogLevel names and sends the selected minimum level in WatchLogs requests.

## TR-LOG-STREAMING-007

**Runtime protocol events use ILogger Debug** — The shared runtime emits Microsoft.Extensions.Logging Debug messages for client request receipt, unary response completion, live tree update sends, live frame sends, log-stream lifecycle, and remote input command completion without logging bearer tokens, property values, or typed text.

## TR-LOG-STREAMING-008

**Android bridge transport diagnostics** — The Android bridge TCP transport emits Debug ILogger diagnostics for accepted client sockets, decoded request frames, sent response frames, and stream completion without logging bearer tokens or payload contents; WatchLogs streams log only lifecycle and completion diagnostics to avoid recursive log generation.

## TR-LOG-STREAMING-009

**Remote provider Debug capture filter** — The remote-control ILoggerProvider is registered with a provider-specific filter that captures Debug and higher log entries for streaming without lowering or replacing filters for existing application logging providers.

## TR-LOG-STREAMING-010

**Desktop client log stream visibility** — The desktop client defaults log verbosity to Warning, starts WatchLogs automatically after a successful connection, restarts the stream when verbosity changes, shows active, stopped, and error state plus entry counts near the log list, and formats rows with sequence, timestamp, level, category, event ID, message, exception summary, structured state, scope, and dropped count when present.

## TR-LOG-STREAMING-011

**Desktop pop-out log window** — The desktop client exposes a pop-out log window backed by the same live observable log collection as the main log panel, preserving active stream state, verbosity behavior, entry counts, and sanitized error display without starting a duplicate WatchLogs stream.

## TR-LOG-STREAMING-012

**Log pop-out ownership state** — The desktop client must track whether the shared log view is embedded or popped out, hide the embedded log list while popped out, and restore it when the pop-out window docks or closes.

## TR-LOG-STREAMING-013

**Shared log verbosity view model** — The desktop client stores selected log verbosity in shared log view state so embedded and pop-out log windows expose synchronized Debug, Information, Warning, and Error selections and stream restarts use the shared minimum level.

## TR-MCP-AIUNIT-001

**Running-tool MCP aiUnit integration scope** — SharpNinja.aiUnit integration tests must validate the avalonia-remote client tool's own in-process MCP server, not the external MCP Server workspace marker contract. Tests must collect evidence from the running tool MCP HTTP host by exercising initialize, tools/list, avalonia_remote_get_capabilities, avalonia_remote_get_snapshot, and avalonia_remote_invoke_click.

## TR-MCP-AIUNIT-002

**aiUnit Codex CLI strategy** — The test project supplies an appsettings.aiunit.json aiUnit strategy named codex-subscription with Kind=cli and Command=codex, causing opt-in live aiUnit MCP Server reviews to run through the operator's installed Codex CLI and existing subscription authentication by default.

## TR-PACK-PACKAGE-001

**Package the server SDK as `Avalonia.RemoteControl.Server`.** — Package the server SDK as `Avalonia.RemoteControl.Server`.

## TR-PACK-PACKAGE-002

**Package the client launcher as `Avalonia.RemoteControl.Tool`.** — Package the client launcher as `Avalonia.RemoteControl.Tool`.

## TR-PACK-PACKAGE-003

**The .NET tool command name is `avalonia-remote`.** — The .NET tool command name is `avalonia-remote`.

## TR-PACK-PACKAGE-004

**Packages include symbols and SourceLink when package infrastructure is implemented.** — Packages include symbols and SourceLink when package infrastructure is implemented.

## TR-PACK-PACKAGE-005

**Runtime package** — Package the host-independent runtime as Avalonia.RemoteControl.Runtime for Android-compatible consumers.

## TR-PLAT-CORE-001

**Target .NET 10.** — Target .NET 10.

## TR-PLAT-CORE-002

**Target Avalonia 12.** — Target Avalonia 12.

## TR-PROP-MUTATION-001

**Property mutation is deny-by-default unless allowed by configured policy.** — Property mutation is deny-by-default unless allowed by configured policy.

## TR-PROP-MUTATION-002

**Supported scalar conversions include string, bool, numeric types, enum, nullable scalar, `Thickness`, `CornerRadius`, `Point`, `Size`, `Rect`, and common color/brush representations when feasible.** — Supported scalar conversions include string, bool, numeric types, enum, nullable scalar, `Thickness`, `CornerRadius`, `Point`, `Size`, `Rect`, and common color/brush representations when feasible.

## TR-PROP-MUTATION-003

**Unsupported property types are reported as unsupported.** — Unsupported property types are reported as unsupported.

## TR-PROP-MUTATION-004

**Indexers, delegates, arbitrary object graphs, services, collections, and private members are blocked in v1.** — Indexers, delegates, arbitrary object graphs, services, collections, and private members are blocked in v1.

## TR-PROP-MUTATION-005

**Failed validation or conversion returns a sanitized error summary.** — Failed validation or conversion returns a sanitized error summary.

## TR-SEC-SECURITY-001

**Server is disabled by default and requires explicit startup/configuration.** — Server is disabled by default and requires explicit startup/configuration.

## TR-SEC-SECURITY-002

**Bearer authentication is required on every RPC, including loopback and ADB tunnel sessions.** — Bearer authentication is required on every RPC, including loopback and ADB tunnel sessions.

## TR-SEC-SECURITY-003

**Tokens are configurable, rotatable, and never written to logs, exceptions, traces, or package artifacts.** — Tokens are configurable, rotatable, and never written to logs, exceptions, traces, or package artifacts.

## TR-SEC-SECURITY-004

**TLS is required for all non-loopback network listeners.** — TLS is required for all non-loopback network listeners.

## TR-SEC-SECURITY-005

**Cleartext h2c is allowed only for loopback or explicitly detected ADB-forwarded localhost sessions.** — Cleartext h2c is allowed only for loopback or explicitly detected ADB-forwarded localhost sessions.

## TR-SEC-SECURITY-006

**Default listener binds to loopback only.** — Default listener binds to loopback only.

## TR-SEC-SECURITY-007

**LAN binding requires explicit endpoint configuration and TLS certificate configuration.** — LAN binding requires explicit endpoint configuration and TLS certificate configuration.

## TR-SEC-SECURITY-008

**Property exposure applies default redaction for sensitive names such as password, token, secret, key, credential, auth, cookie, and connection string.** — Property exposure applies default redaction for sensitive names such as password, token, secret, key, credential, auth, cookie, and connection string.

## TR-SEC-SECURITY-009

**Log streaming applies the same sensitive-data redaction policy.** — Log streaming applies the same sensitive-data redaction policy.

## TR-SEC-SECURITY-010

**Property mutation is deny-by-default unless allowed by configured policy.** — Property mutation is deny-by-default unless allowed by configured policy.

## TR-SEC-SECURITY-011

**All mutation commands pass through a command authorization policy.** — All mutation commands pass through a command authorization policy.

## TR-SEC-SECURITY-012

**Mutation and action failures return safe error summaries instead of raw exception dumps.** — Mutation and action failures return safe error summaries instead of raw exception dumps.

## TR-SEC-SECURITY-013

**Every remote mutation/action emits an audit log with timestamp, authenticated client identity, node ID, command type, result, and sanitized details.** — Every remote mutation/action emits an audit log with timestamp, authenticated client identity, node ID, command type, result, and sanitized details.

## TR-SEC-SECURITY-014

**Rejected authentication, rejected authorization, blocked property access, and failed mutation attempts emit audit logs.** — Rejected authentication, rejected authorization, blocked property access, and failed mutation attempts emit audit logs.

## TR-SEC-SECURITY-015

**ADB forwarding cleanup runs by default when the client disconnects.** — ADB forwarding cleanup runs by default when the client disconnects.

## TR-SEC-SECURITY-016

**Client persists connection settings only in user-scoped storage and never logs tokens.** — Client persists connection settings only in user-scoped storage and never logs tokens.

## TR-SEC-SECURITY-017

**Persist accepted TLS fingerprints** — Manually accepted TLS certificates are persisted as SHA-256 certificate fingerprints and connections succeed only when the presented server certificate matches the accepted fingerprint or configured certificate file.

## TR-SEC-SECURITY-018

**Live frame security gate** — Live frame streaming must be disabled by default and rejected unless AllowRemoteFrames is enabled.

## TR-SEC-SECURITY-019

**Live input security gate and audit** — Live remote input must be disabled by default, require AllowRemoteActions and AllowRemoteInput, and emit sanitized audit logs without typed text.

## TR-UI-RUNTIME-001

**All Avalonia tree, property, and action access runs through the Avalonia UI dispatcher.** — All Avalonia tree, property, and action access runs through the Avalonia UI dispatcher.

## TR-UI-RUNTIME-002

**Snapshot capture handles top-level windows.** — Snapshot capture handles top-level windows.

## TR-UI-RUNTIME-003

**Snapshot capture handles popups/flyouts when Avalonia exposes them safely.** — Snapshot capture handles popups/flyouts when Avalonia exposes them safely.

## TR-UI-RUNTIME-004

**Virtualized or unrealized items are represented as unavailable rather than fabricated.** — Virtualized or unrealized items are represented as unavailable rather than fabricated.

## TR-UI-RUNTIME-005

**Stale node IDs are detected and reported.** — Stale node IDs are detected and reported.

## TR-UI-RUNTIME-006

**Absolute bounds for live view** — Tree snapshots must expose root-relative absolute bounds while preserving existing local bounds values.

## TR-UI-RUNTIME-007

**Dispatcher-safe frame capture** — Frame capture must run on the Avalonia UI dispatcher using RenderTargetBitmap, enforce max frame size, and support cancellation-aware periodic streaming.

## TR-UI-RUNTIME-008

**Normalize capture and input roots to TopLevel** — Runtime frame capture, live tree snapshots, and live input dispatch must normalize an app-provided Control root to its containing Avalonia TopLevel before rendering, traversing, or dispatching input.

