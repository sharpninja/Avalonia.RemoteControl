# Technical Requirements

## Platform and Packaging

- `TR-PLAT-001`: Target .NET 10.
- `TR-PLAT-002`: Target Avalonia 12.
- `TR-PACK-001`: Package the server SDK as `Avalonia.RemoteControl.Server`.
- `TR-PACK-002`: Package the client launcher as `Avalonia.RemoteControl.Tool`.
- `TR-PACK-003`: The .NET tool command name is `avalonia-remote`.
- `TR-PACK-004`: Packages include symbols and SourceLink when package infrastructure is implemented.

## Protocol

- `TR-GRPC-001`: Define a versioned protobuf contract for desktop-facing communication.
- `TR-GRPC-002`: Provide `GetCapabilities`.
- `TR-GRPC-003`: Provide `GetSnapshot`.
- `TR-GRPC-004`: Provide `WatchTree`.
- `TR-GRPC-005`: Provide `InvokeClick`.
- `TR-GRPC-006`: Provide `SetProperty`.
- `TR-GRPC-007`: Provide `WatchLogs`.
- `TR-GRPC-008`: Streaming responses include sequence/version data sufficient for reconnect and resync behavior.

## Dependency Injection and Hosting

- `TR-DI-001`: Expose `IServiceCollection` integration for server registration.
- `TR-DI-002`: Expose `IServiceProvider` startup integration.
- `TR-DI-003`: Expose `AvaloniaRemoteControlOptions`.
- `TR-DI-004`: Integrate with application lifetime so the server starts and stops cleanly.
- `TR-DI-005`: Do not replace or suppress existing application logging providers.

## Avalonia Runtime Access

- `TR-UI-001`: All Avalonia tree, property, and action access runs through the Avalonia UI dispatcher.
- `TR-UI-002`: Snapshot capture handles top-level windows.
- `TR-UI-003`: Snapshot capture handles popups/flyouts when Avalonia exposes them safely.
- `TR-UI-004`: Virtualized or unrealized items are represented as unavailable rather than fabricated.
- `TR-UI-005`: Stale node IDs are detected and reported.

## Property Mutation

- `TR-PROP-001`: Property mutation is deny-by-default unless allowed by configured policy.
- `TR-PROP-002`: Supported scalar conversions include string, bool, numeric types, enum, nullable scalar, `Thickness`, `CornerRadius`, `Point`, `Size`, `Rect`, and common color/brush representations when feasible.
- `TR-PROP-003`: Unsupported property types are reported as unsupported.
- `TR-PROP-004`: Indexers, delegates, arbitrary object graphs, services, collections, and private members are blocked in v1.
- `TR-PROP-005`: Failed validation or conversion returns a sanitized error summary.

## Actions

- `TR-ACTION-001`: Click invocation runs on the Avalonia UI dispatcher.
- `TR-ACTION-002`: Click invocation uses the visible center of the selected node by default.
- `TR-ACTION-003`: Command-control semantic invocation may be used when pointer event synthesis is not appropriate.
- `TR-ACTION-004`: Unsupported gestures, text input, drag/drop, and arbitrary method invocation are out of v1 unless added through future requirements.

## Logging

- `TR-LOG-001`: Implement a bounded `ILoggerProvider`.
- `TR-LOG-002`: Log streaming captures timestamp, level, category, event ID, rendered message, structured state summary, scope summary, exception summary, sequence number, and dropped count.
- `TR-LOG-003`: Buffer limits are configurable.
- `TR-LOG-004`: Dropped messages are counted and surfaced to clients.
- `TR-LOG-005`: Log streaming applies sensitive-data redaction.

## Android ADB Connectivity

- `TR-ADB-001`: Add an ADB connection profile to `avalonia-remote`.
- `TR-ADB-002`: Use `adb forward tcp:<hostPort> tcp:<devicePort>` for host-client-to-device-app connections.
- `TR-ADB-003`: Support `adb -s <serial>` everywhere.
- `TR-ADB-004`: Store Android debug endpoint metadata in a debuggable-app-accessible marker or equivalent discovery mechanism.
- `TR-ADB-005`: Provide explicit fallback flags for package, port, token, and certificate mode.
- `TR-ADB-006`: Keep bearer authentication required over ADB tunnels.
- `TR-ADB-007`: Clean up ADB forwards by default when the client disconnects.
- `TR-ADB-008`: Technical Spike 0 must prove the chosen Android app-side transport before implementation depends on it.

## Security Constraints

- `TR-SEC-001`: Server is disabled by default and requires explicit startup/configuration.
- `TR-SEC-002`: Bearer authentication is required on every RPC, including loopback and ADB tunnel sessions.
- `TR-SEC-003`: Tokens are configurable, rotatable, and never written to logs, exceptions, traces, or package artifacts.
- `TR-SEC-004`: TLS is required for all non-loopback network listeners.
- `TR-SEC-005`: Cleartext h2c is allowed only for loopback or explicitly detected ADB-forwarded localhost sessions.
- `TR-SEC-006`: Default listener binds to loopback only.
- `TR-SEC-007`: LAN binding requires explicit endpoint configuration and TLS certificate configuration.
- `TR-SEC-008`: Property exposure applies default redaction for sensitive names such as password, token, secret, key, credential, auth, cookie, and connection string.
- `TR-SEC-009`: Log streaming applies the same sensitive-data redaction policy.
- `TR-SEC-010`: Property mutation is deny-by-default unless allowed by configured policy.
- `TR-SEC-011`: All mutation commands pass through a command authorization policy.
- `TR-SEC-012`: Mutation and action failures return safe error summaries instead of raw exception dumps.
- `TR-SEC-013`: Every remote mutation/action emits an audit log with timestamp, authenticated client identity, node ID, command type, result, and sanitized details.
- `TR-SEC-014`: Rejected authentication, rejected authorization, blocked property access, and failed mutation attempts emit audit logs.
- `TR-SEC-015`: ADB forwarding cleanup runs by default when the client disconnects.
- `TR-SEC-016`: Client persists connection settings only in user-scoped storage and never logs tokens.

## CI and Release

- `TR-CI-001`: GitHub Actions restores, builds, tests, packs, and uploads artifacts.
- `TR-CI-002`: Azure Pipelines restores, builds, tests, packs, and uploads artifacts.
- `TR-CI-003`: Tagged `v*` releases publish packages through protected secrets/service connections.
- `TR-CI-004`: Duplicate publish prevention exists across GitHub and Azure release paths.
- `TR-CI-005`: Public GitHub and private Azure DevOps source-of-truth policy is documented before first package publish.
