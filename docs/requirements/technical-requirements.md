# Technical Requirements

## Platform and Packaging

- `TR-PLAT-CORE-001`: Target .NET 10.
- `TR-PLAT-CORE-002`: Target Avalonia 12.
- `TR-PACK-PACKAGE-001`: Package the server SDK as `Avalonia.RemoteControl.Server`.
- `TR-PACK-PACKAGE-002`: Package the client launcher as `Avalonia.RemoteControl.Tool`.
- `TR-PACK-PACKAGE-003`: The .NET tool command name is `avalonia-remote`.
- `TR-PACK-PACKAGE-004`: Packages include symbols and SourceLink when package infrastructure is implemented.
- `TR-PACK-PACKAGE-005`: Package the host-independent runtime as `Avalonia.RemoteControl.Runtime` for Android-compatible consumers.

## Protocol

- `TR-GRPC-PROTOCOL-001`: Define a versioned protobuf contract for desktop-facing communication.
- `TR-GRPC-PROTOCOL-002`: Provide `GetCapabilities`.
- `TR-GRPC-PROTOCOL-003`: Provide `GetSnapshot`.
- `TR-GRPC-PROTOCOL-004`: Provide `WatchTree`.
- `TR-GRPC-PROTOCOL-005`: Provide `InvokeClick`.
- `TR-GRPC-PROTOCOL-006`: Provide `SetProperty`.
- `TR-GRPC-PROTOCOL-007`: Provide `WatchLogs`.
- `TR-GRPC-PROTOCOL-008`: Streaming responses include sequence/version data sufficient for reconnect and resync behavior.

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
- `TR-ACTION-INVOCATION-004`: Unsupported gestures, text input, drag/drop, and arbitrary method invocation are out of v1 unless added through future requirements.

## Logging

- `TR-LOG-STREAMING-001`: Implement a bounded `ILoggerProvider`.
- `TR-LOG-STREAMING-002`: Log streaming captures timestamp, level, category, event ID, rendered message, structured state summary, scope summary, exception summary, sequence number, and dropped count.
- `TR-LOG-STREAMING-003`: Buffer limits are configurable.
- `TR-LOG-STREAMING-004`: Dropped messages are counted and surfaced to clients.
- `TR-LOG-STREAMING-005`: Log streaming applies sensitive-data redaction.

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

## CI and Release

- `TR-CI-RELEASE-001`: GitHub Actions restores, builds, tests, packs, and uploads artifacts.
- `TR-CI-RELEASE-002`: Azure Pipelines restores, builds, tests, packs, and uploads artifacts.
- `TR-CI-RELEASE-003`: Tagged `v*` releases publish packages through protected secrets/service connections.
- `TR-CI-RELEASE-004`: Duplicate publish prevention exists across GitHub and Azure release paths.
- `TR-CI-RELEASE-005`: Public GitHub and private Azure DevOps source-of-truth policy is documented before first package publish.
