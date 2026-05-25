# Traceability Matrix

This matrix is intentionally started before implementation. It maps requirements to planned tests, evidence, and owning iterations. Implementation must keep it current.

## Iteration 0 - Foundation

- Product definition: `docs/requirements/product.md`
- Process definition: `docs/Development-Process.md`
- Agent instructions: `AGENTS.md`
- Workspace build guidance: `.github/copilot-instructions.md`
- Architecture baseline: `docs/architecture/overview.md`

Evidence required:

- Documentation files exist.
- Requirements have IDs.
- Technical Spike 0 is defined before transport implementation begins.
- Solution skeleton exists in `Avalonia.RemoteControl.slnx`.
- Initial package metadata exists in `Directory.Build.props` and `Directory.Packages.props`.
- GitHub Actions and Azure Pipelines validation scaffolds exist.
- Initial test project proves security defaults, DI registration, client modes, and tool help.
- Release build, test, pack, and local tool install gates pass before moving to Iteration 1.

Implemented evidence:

- `Avalonia.RemoteControl.slnx` exists with protocol, server, client, tool, sample, and test projects.
- `.github/workflows/ci.yml` and `azure-pipelines.yml` run restore/build/test/pack.
- Foundation tests cover default-disabled server options, DI registration, startup-state sanitization, client modes, and tool help.

## Technical Spike 0 - Android Transport Proof

Requirements:

- `FR-ADB-001`
- `FR-ADB-002`
- `FR-ADB-003`
- `FR-ADB-004`
- `FR-ADB-005`
- `FR-ADB-006`
- `TR-ADB-CONNECTIVITY-001`
- `TR-ADB-CONNECTIVITY-002`
- `TR-ADB-CONNECTIVITY-003`
- `TR-ADB-CONNECTIVITY-004`
- `TR-ADB-CONNECTIVITY-005`
- `TR-ADB-CONNECTIVITY-006`
- `TR-ADB-CONNECTIVITY-007`
- `TR-ADB-CONNECTIVITY-008`
- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-010`
- `TR-ADB-CONNECTIVITY-011`
- `TR-ADB-CONNECTIVITY-012`
- `TR-ADB-CONNECTIVITY-013`
- `TR-ADB-CONNECTIVITY-014`
- `TR-ADB-CONNECTIVITY-015`
- `TR-SEC-SECURITY-002`
- `TR-SEC-SECURITY-005`

Tests/evidence:

- `TEST-ADB-001`
- `TEST-ADB-002`
- `TEST-ADB-003`
- `TEST-ADB-004`
- `TEST-ADB-005`
- `TEST-ADB-006`
- `TEST-ADB-007`
- `TEST-ADB-008`
- `TEST-ADB-009`
- `TEST-ADB-010`
- `TEST-ADB-011`
- documented decision on Android app-side transport

Evidence:

- `adb version`, `adb devices -l`, and `avalonia-remote adb list` validated host-side ADB availability against a connected Android device.
- Package marker discovery against a non-debuggable package failed with Android's expected `run-as: package not debuggable` protection.
- A generated Avalonia `net10.0-android` app referencing `Avalonia.RemoteControl.Server` failed packaging with `NETSDK1082` because `Microsoft.AspNetCore.App` has no `android-arm64` runtime pack.
- Decision: Android app-side support needs an Android-compatible bridge or transport behind the same desktop-facing protocol; the current AspNetCore/Kestrel server remains viable for desktop/server-capable targets only.
- `Avalonia.RemoteControl.Runtime` targets `net10.0-android` and builds without `Microsoft.AspNetCore.App`, Kestrel, or `Grpc.AspNetCore`.
- `RemoteControlBridgeTcpListenerTests` covers loopback binding, authenticated unary bridge requests, snapshot response transport, marker creation, and package-private marker JSON writing.
- `samples/Avalonia.RemoteControl.AndroidProbe.Android` builds directly for `net10.0-android` and starts the runtime bridge listener without adding ASP.NET Core/Kestrel dependencies.
- Physical Android device `ZD222QH58Q` installed and launched the probe package, exposed a package-private `arc-protobuf-v1` marker through `run-as`, completed `avalonia-remote adb connect --keep-forward`, returned a live 31-node snapshot over the forwarded bridge, and removed the ADB forward with `avalonia-remote adb cleanup`.

## Iteration 1 - Protocol and Read-Only Inspection

Requirements:

- `FR-TREE-001`
- `FR-TREE-002`
- `FR-TREE-003`
- `FR-PROP-001`
- `FR-PROP-002`
- `FR-SEC-001`
- `FR-SEC-002`
- `FR-SEC-003`
- `FR-SEC-005`
- `FR-SEC-008`
- `TR-GRPC-PROTOCOL-001`
- `TR-GRPC-PROTOCOL-002`
- `TR-GRPC-PROTOCOL-003`
- `TR-DI-HOSTING-001`
- `TR-DI-HOSTING-002`
- `TR-DI-HOSTING-003`
- `TR-UI-RUNTIME-001`
- `TR-UI-RUNTIME-002`
- `TR-UI-RUNTIME-003`
- `TR-UI-RUNTIME-004`
- `TR-SEC-SECURITY-001`
- `TR-SEC-SECURITY-002`
- `TR-SEC-SECURITY-003`
- `TR-SEC-SECURITY-004`
- `TR-SEC-SECURITY-006`
- `TR-SEC-SECURITY-007`
- `TR-SEC-SECURITY-008`

Tests/evidence:

- `TEST-UNIT-001`
- `TEST-UNIT-002`
- `TEST-UNIT-006`
- `TEST-AVA-001`
- `TEST-GRPC-001`
- `TEST-GRPC-002`
- `TEST-GRPC-003`
- `TEST-SEC-001`
- `TEST-SEC-002`
- `TEST-SEC-004`

Implemented evidence:

- `RemoteControlReadOnlyInspectionTests` covers capabilities, stable node IDs, hierarchy, automation metadata, classes, and sensitive property redaction.
- `AvaloniaControlTreeSnapshotProvider` captures snapshots through `IRemoteControlDispatcher`.
- `GetCapabilities` and `GetSnapshot` are mapped through `AvaloniaRemoteControlGrpcService`.
- `RemoteControlSecurityTests` covers bearer-token authentication and startup policy validation for loopback, non-loopback, and ADB tunnel modes.
- `RemoteControlSecurityTests` covers authenticated client identity stamping in gRPC call state.
- `RemoteControlHostedServerTests` proves the hosted gRPC endpoint rejects unauthenticated calls and serves authenticated calls.
- `AvaloniaRemoteControlServerHost` starts the HTTP/2 gRPC transport from `IServiceProvider` services.
- `RemoteControlHostingTests` covers the `IServiceProvider.StartAvaloniaRemoteControlAsync` and `StopAvaloniaRemoteControlAsync` helper path when the server is disabled.
- `RemoteControlHostingTests` covers `IControlledApplicationLifetime.AttachAvaloniaRemoteControl`, proving startup starts the server, exit stops it, and disposing the registration detaches event handlers.

## Iteration 2 - Live Updates and Remote Actions

Requirements:

- `FR-TREE-004`
- `FR-TREE-005`
- `FR-PROP-003`
- `FR-PROP-004`
- `FR-PROP-005`
- `FR-ACTION-001`
- `FR-ACTION-002`
- `FR-ACTION-003`
- `FR-ACTION-004`
- `FR-SEC-006`
- `FR-SEC-007`
- `TR-GRPC-PROTOCOL-004`
- `TR-GRPC-PROTOCOL-005`
- `TR-GRPC-PROTOCOL-006`
- `TR-GRPC-PROTOCOL-008`
- `TR-UI-RUNTIME-005`
- `TR-PROP-MUTATION-001`
- `TR-PROP-MUTATION-002`
- `TR-PROP-MUTATION-003`
- `TR-PROP-MUTATION-004`
- `TR-PROP-MUTATION-005`
- `TR-ACTION-INVOCATION-001`
- `TR-ACTION-INVOCATION-002`
- `TR-ACTION-INVOCATION-003`
- `TR-ACTION-INVOCATION-004`
- `TR-SEC-SECURITY-010`
- `TR-SEC-SECURITY-011`
- `TR-SEC-SECURITY-012`
- `TR-SEC-SECURITY-013`
- `TR-SEC-SECURITY-014`

Tests/evidence:

- `TEST-UNIT-003`
- `TEST-UNIT-004`
- `TEST-AVA-002`
- `TEST-AVA-003`
- `TEST-AVA-004`
- `TEST-GRPC-004`
- `TEST-GRPC-006`
- `TEST-GRPC-007`
- `TEST-SEC-005`
- `TEST-SEC-006`
- `TEST-SEC-007`

Implemented evidence:

- `RemoteControlTreeStreamTests` covers periodic live snapshot streaming and cancellation.
- `RemoteControlCommandTests` covers deny-by-default mutation, allow-listed mutation, sensitive mutation blocking, guarded click invocation, and gRPC command mapping.
- `RemoteControlCommandTests` covers guarded focus invocation and gRPC focus command mapping.
- `RemoteControlCommandTests` covers configured mutation for string, `Thickness`, `CornerRadius`, `Point`, `Size`, `Rect`, and solid color brush values.
- `RemoteControlCommandTests` covers action and mutation audit log messages containing sanitized client identity.
- `RemoteControlCommandTests` covers button semantic click invocation and non-button surface click invocation through center-position `PointerPressed`, `PointerReleased`, and typed `Tapped` routed events.

## Iteration 3 - Logging

Requirements:

- `FR-LOG-001`
- `FR-LOG-002`
- `FR-LOG-003`
- `FR-LOG-004`
- `FR-LOG-005`
- `FR-LOG-006`
- `TR-GRPC-PROTOCOL-007`
- `TR-DI-HOSTING-005`
- `TR-LOG-STREAMING-001`
- `TR-LOG-STREAMING-002`
- `TR-LOG-STREAMING-003`
- `TR-LOG-STREAMING-004`
- `TR-LOG-STREAMING-005`
- `TR-LOG-STREAMING-006`
- `TR-LOG-STREAMING-007`
- `TR-SEC-SECURITY-009`

Tests/evidence:

- `TEST-UNIT-005`
- `TEST-LOG-001`
- `TEST-CLIENT-002`
- `TEST-GRPC-005`
- `TEST-SEC-004`

Implemented evidence:

- `RemoteControlLoggingTests` covers remote logger provider registration, sensitive log redaction, buffer drop accounting, and level/category filtering.
- `RemoteControlLogBuffer` is a bounded replay buffer with cumulative dropped-entry counts.
- `RemoteControlLoggerProvider` captures `ILogger` messages without replacing existing logging providers.
- `WatchLogs` is defined on the gRPC service and maps sanitized log entries to protocol messages.
- `RemoteControlDesktopSessionTests` covers hosted gRPC log streaming from the server log buffer to the desktop client session.
- `RemoteControlLoggingTests` covers the supported client log verbosity options and the minimum-level names sent to `WatchLogs`.
- `RemoteControlProtocolEventLoggingTests` covers Debug `ILogger` messages for runtime client request receipt, unary responses, stream updates, and log-stream lifecycle without echoing each outgoing log entry.

## Iteration 4 - Client and Tool

Requirements:

- `FR-CLIENT-001`
- `FR-CLIENT-002`
- `FR-CLIENT-003`
- `FR-CLIENT-004`
- `FR-CLIENT-005`
- `FR-CLIENT-006`
- `FR-CLIENT-007`
- `FR-SEC-009`
- `FR-SEC-010`
- `TR-PACK-PACKAGE-002`
- `TR-PACK-PACKAGE-003`
- `TR-SEC-SECURITY-016`
- `TR-SEC-SECURITY-017`
- `TR-ADB-CONNECTIVITY-016`

Tests/evidence:

- `TEST-PACK-002`
- `TEST-PACK-003`
- `TEST-PACK-004`
- `TEST-SEC-008`
- `TEST-ADB-012`
- `TEST-MANUAL-001`
- `TEST-MANUAL-004`
- `TEST-MANUAL-005`
- `TEST-MANUAL-006`
- `TEST-MANUAL-007`

Implemented evidence:

- `RemoteControlDesktopSessionTests` covers authenticated desktop client session capability probing against the hosted server.
- `RemoteControlDesktopSession` provides authenticated gRPC client calls for capabilities, snapshots, click invocation, property mutation, and log streaming.
- `Avalonia.RemoteControl.Tool` launches a basic Avalonia desktop UI when run without arguments.
- The desktop UI includes endpoint/token connection controls, tree rendering, selected-node properties, invoke-click, set-property, log streaming with selectable verbosity, and status feedback.
- `RemoteControlProfileStoreTests` covers saving, loading, and forgetting the default user-scoped connection profile.
- The desktop UI exposes Save and Forget controls for endpoint/token/certificate-path/fingerprint profile state.
- `RemoteControlDesktopSessionTests` covers connecting to a hosted TLS endpoint with a configured trusted server certificate file.
- `RemoteControlDesktopSessionTests` covers accepted SHA-256 fingerprint trust, mismatched fingerprint rejection, and certificate inspection.
- `RemoteControlProfileStoreTests` covers accepted SHA-256 fingerprint persistence and deletion through profile forget.
- The desktop UI exposes inspect, accept, and reject controls for manual TLS certificate trust.
- The desktop UI and profile store preserve the selected transport protocol so kept ADB bridge forwards can be reopened with `arc-protobuf-v1`.
- `docs/requirements/manual-acceptance-evidence.md` records the current manual acceptance evidence for loopback, TLS/token, live tree, logs, click, property edit, audit trail, and Android ADB workflows.

## Iteration 5 - ADB Client UX

Requirements:

- `FR-ADB-001`
- `FR-ADB-002`
- `FR-ADB-003`
- `FR-ADB-004`
- `FR-ADB-005`
- `FR-ADB-006`
- `FR-SEC-004`
- `TR-ADB-CONNECTIVITY-001`
- `TR-ADB-CONNECTIVITY-002`
- `TR-ADB-CONNECTIVITY-003`
- `TR-ADB-CONNECTIVITY-004`
- `TR-ADB-CONNECTIVITY-005`
- `TR-ADB-CONNECTIVITY-006`
- `TR-ADB-CONNECTIVITY-007`
- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-016`
- `TR-SEC-SECURITY-015`

Tests/evidence:

- `TEST-ADB-001`
- `TEST-ADB-002`
- `TEST-ADB-003`
- `TEST-ADB-004`
- `TEST-ADB-005`
- `TEST-MANUAL-003`
- `TEST-ADB-012`

Implemented evidence:

- `RemoteControlAdbClientTests` covers `adb devices -l` parsing, ADB device listing, serial-specific port forwarding, forward cleanup, package marker discovery, and CLI connect cleanup behavior.
- `RemoteControlAdbClientTests` covers saving a transport-aware default profile after `adb connect --keep-forward`.
- `AdbClient` creates `adb -s <serial> forward tcp:<hostPort> tcp:<devicePort>` and removes forwards with `adb -s <serial> forward --remove tcp:<hostPort>`.
- `AdbCommandLine` wires `adb list`, `adb connect`, and `adb cleanup` into the .NET tool workflow.
- `GrpcRemoteControlProbe` authenticates `GetCapabilities` over the forwarded localhost endpoint.
- Physical-device acceptance for the Android bridge probe is recorded under Technical Spike 0 and `TEST-MANUAL-003`; broader emulator/device matrix coverage remains future compatibility work.

## Iteration 9 - Live Interactive Remote View

Requirements:

- `FR-CLIENT-008`
- `FR-CLIENT-009`
- `FR-CLIENT-010`
- `FR-ACTION-005`
- `FR-SEC-011`
- `FR-SEC-012`
- `TR-GRPC-PROTOCOL-009`
- `TR-GRPC-PROTOCOL-010`
- `TR-UI-RUNTIME-006`
- `TR-UI-RUNTIME-007`
- `TR-UI-RUNTIME-008`
- `TR-ACTION-INVOCATION-005`
- `TR-SEC-SECURITY-018`
- `TR-SEC-SECURITY-019`
- `TR-ADB-CONNECTIVITY-017`

Tests/evidence:

- `TEST-CLIENT-001`
- `TEST-GRPC-008`
- `TEST-GRPC-009`
- `TEST-ADB-013`
- `TEST-AVA-005`
- `TEST-AVA-006`
- `TEST-AVA-007`
- additive protocol contract tests
- frame stream runtime and transport tests
- remote input policy and audit tests
- desktop live-view model tests

Evidence required:

- `WatchFrames` and `SendInput` are additive protocol members and older snapshot/action APIs remain compatible.
- Frame streaming is rejected by default and succeeds only when live frames are enabled.
- Remote input is rejected by default and succeeds only when both remote actions and remote input are enabled.
- Tree snapshots include absolute bounds while preserving existing local bounds.
- Frame capture, tree snapshots, and input dispatch normalize child roots to the containing `TopLevel` so target-device backgrounds, popups, flyouts, and overlays are visible and interactive.
- gRPC and Android bridge transports both support live tree/frame streaming and remote input.
- The desktop client opens a separate live-view window with screenshot and tree replica modes.

## Iteration 7 - Android Bridge Transport

Requirements:

- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-010`
- `TR-ADB-CONNECTIVITY-011`
- `TR-ADB-CONNECTIVITY-012`
- `TR-ADB-CONNECTIVITY-013`
- `TR-ADB-CONNECTIVITY-014`
- `TR-SEC-SECURITY-002`
- `TR-SEC-SECURITY-005`

Tests/evidence:

- `TEST-ADB-006`
- `TEST-ADB-007`
- `TEST-ADB-008`
- `TEST-ADB-009`
- `TEST-ADB-010`
- `TEST-ADB-011`
- `TEST-MANUAL-003`
- Android bridge sample package
- package marker read through `adb shell run-as`
- authenticated capability probe through `adb forward`
- tree snapshot captured on the Avalonia dispatcher
- fail-closed unsupported marker transport test
- length-prefixed protobuf bridge envelope unit tests
- Android-compatible runtime build check
- host-side bridge client adapter test
- cleanup of the created ADB forward

Implemented evidence:

- Android marker parsing now recognizes versioned protocol metadata, keeps missing metadata compatible with legacy `grpc`, supports `arc-protobuf-v1`, and rejects unknown protocols before creating a forward.
- `RemoteControlBridgeProtocolTests` covers `arc-protobuf-v1` transport constants, `BridgeRequest`/`BridgeResponse` length-prefixed frame round-trip, oversized frame rejection, and sanitized failure response shape.
- `RemoteControlBridgeRequestHandlerTests` covers bridge bearer authentication, capabilities, snapshot dispatch, and property mutation through the runtime policy.
- `RemoteControlDesktopSessionTests` covers capabilities over a loopback TCP `arc-protobuf-v1` bridge connection.
- `RemoteControlAdbClientTests` covers marker-discovered `arc-protobuf-v1` ADB connect flow and protocol handoff to probing.
- App-side bridge listener tests, Android probe build evidence, and physical device acceptance cover `TR-ADB-CONNECTIVITY-015`, `TEST-ADB-011`, and `TEST-MANUAL-003`.
- Technical Spike 0 rejected ASP.NET Core/Kestrel gRPC as the Android app-side transport and created the bridge requirement.

## Iteration 6 - CI, Packaging, and Release

Requirements:

- `TR-PACK-PACKAGE-001`
- `TR-PACK-PACKAGE-004`
- `TR-PACK-PACKAGE-005`
- `TR-PACK-PACKAGE-006`
- `TR-CI-RELEASE-001`
- `TR-CI-RELEASE-002`
- `TR-CI-RELEASE-003`
- `TR-CI-RELEASE-004`
- `TR-CI-RELEASE-005`

Tests/evidence:

- `TEST-PACK-001`
- `TEST-PACK-002`
- `TEST-PACK-005`
- `TEST-PACK-006`
- `TEST-CI-001`
- `TEST-CI-002`
- package artifacts
- tagged release dry run before first public publish

Implemented evidence:

- `Directory.Build.props` enables repository URL metadata, SourceLink, embedded untracked sources, `.nupkg`, and `.snupkg` package outputs.
- `SharpNinja.Avalonia.RemoteControl.Protocol` is packable so Runtime and Server package dependencies resolve from NuGet.
- `SharpNinja.Avalonia.RemoteControl.Runtime` is packable and supplies the Android-compatible runtime dependency for future bridge debuggee packages.
- `Microsoft.SourceLink.GitHub` is centrally versioned in `Directory.Packages.props`.
- `.github/workflows/ci.yml` restores, builds, tests, packs, uploads artifacts, and publishes tagged `v*` packages only when `NUGET_API_KEY` is configured.
- `azure-pipelines.yml` restores, builds, tests, packs, publishes build artifacts, and can publish tagged `v*` packages only when `NuGetApiKey` is configured.
- `docs/release.md` documents GitHub as public release source of truth, Azure as private validation/mirror, tagged release shape, owner-controlled `SharpNinja.Avalonia.RemoteControl.*` package IDs, and duplicate publish prevention through `--skip-duplicate`.
- Azure Pipelines definition `Avalonia.RemoteControl-CI` has run successfully against `master`; GitHub Actions is defined but current hosted runs are blocked before job start by the GitHub account billing lock.

## Iteration 8 - User Documentation

Requirements:

- `TR-DOC-USER-001`

Tests/evidence:

- `TEST-DOC-001`
- README user-doc links
- user documentation files under `docs/user`

Implemented evidence:

- `docs/user/index.md` provides the user documentation entry point.
- `docs/user/getting-started.md` covers tool installation, server package installation, loopback server setup, and first connection.
- `docs/user/server-integration.md` covers service registration, root providers, options, mutation policy, logging, TLS, and manual start/stop.
- `docs/user/client-tool.md` covers desktop client launch, connection fields, saved profiles, tree inspection, actions, property edits, and logs.
- `docs/user/android-adb.md` covers ADB device listing, explicit port/token connection, package marker discovery, cleanup, and Android bridge responsibilities.
- `docs/user/security.md` documents the safety model, tokens, TLS, mutation policy, redaction, ADB posture, and enablement checklist.
- `docs/user/troubleshooting.md` covers common install, startup, connection, authentication, tree, mutation, action, logging, ADB, and NuGet package-name issues.
