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
- `TR-ADB-001`
- `TR-ADB-002`
- `TR-ADB-003`
- `TR-ADB-004`
- `TR-ADB-005`
- `TR-ADB-006`
- `TR-ADB-007`
- `TR-ADB-008`
- `TR-SEC-002`
- `TR-SEC-005`

Tests/evidence:

- `TEST-ADB-001`
- `TEST-ADB-002`
- `TEST-ADB-003`
- `TEST-ADB-004`
- `TEST-ADB-005`
- `TEST-ADB-006`
- documented decision on Android app-side transport

## Iteration 1 - Protocol and Read-Only Inspection

Requirements:

- `FR-TREE-001`
- `FR-TREE-002`
- `FR-TREE-003`
- `FR-PROP-001`
- `FR-PROP-002`
- `FR-SEC-001`
- `FR-SEC-003`
- `FR-SEC-005`
- `FR-SEC-008`
- `TR-GRPC-001`
- `TR-GRPC-002`
- `TR-GRPC-003`
- `TR-DI-001`
- `TR-DI-002`
- `TR-DI-003`
- `TR-UI-001`
- `TR-UI-002`
- `TR-UI-003`
- `TR-UI-004`
- `TR-SEC-001`
- `TR-SEC-002`
- `TR-SEC-003`
- `TR-SEC-004`
- `TR-SEC-006`
- `TR-SEC-007`
- `TR-SEC-008`

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
- `TR-GRPC-004`
- `TR-GRPC-005`
- `TR-GRPC-006`
- `TR-GRPC-008`
- `TR-UI-005`
- `TR-PROP-001`
- `TR-PROP-002`
- `TR-PROP-003`
- `TR-PROP-004`
- `TR-PROP-005`
- `TR-ACTION-001`
- `TR-ACTION-002`
- `TR-ACTION-003`
- `TR-ACTION-004`
- `TR-SEC-010`
- `TR-SEC-011`
- `TR-SEC-012`
- `TR-SEC-013`
- `TR-SEC-014`

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
- Focus invocation and pointer-center synthesis remain planned.

## Iteration 3 - Logging

Requirements:

- `FR-LOG-001`
- `FR-LOG-002`
- `FR-LOG-003`
- `FR-LOG-004`
- `TR-GRPC-007`
- `TR-DI-005`
- `TR-LOG-001`
- `TR-LOG-002`
- `TR-LOG-003`
- `TR-LOG-004`
- `TR-LOG-005`
- `TR-SEC-009`

Tests/evidence:

- `TEST-UNIT-005`
- `TEST-GRPC-005`
- `TEST-SEC-004`

Implemented evidence:

- `RemoteControlLoggingTests` covers remote logger provider registration, sensitive log redaction, buffer drop accounting, and level/category filtering.
- `RemoteControlLogBuffer` is a bounded replay buffer with cumulative dropped-entry counts.
- `RemoteControlLoggerProvider` captures `ILogger` messages without replacing existing logging providers.
- `WatchLogs` is defined on the gRPC service and maps sanitized log entries to protocol messages.
- Full hosted gRPC log-stream integration remains part of later transport/server hosting validation.

## Iteration 4 - Client and Tool

Requirements:

- `FR-CLIENT-001`
- `FR-CLIENT-002`
- `FR-CLIENT-003`
- `FR-CLIENT-004`
- `FR-CLIENT-005`
- `FR-CLIENT-006`
- `FR-SEC-009`
- `TR-PACK-002`
- `TR-PACK-003`
- `TR-SEC-016`

Tests/evidence:

- `TEST-PACK-002`
- `TEST-PACK-003`
- `TEST-PACK-004`
- `TEST-MANUAL-001`
- `TEST-MANUAL-004`
- `TEST-MANUAL-005`
- `TEST-MANUAL-006`
- `TEST-MANUAL-007`

## Iteration 5 - ADB Client UX

Requirements:

- `FR-ADB-001`
- `FR-ADB-002`
- `FR-ADB-003`
- `FR-ADB-004`
- `FR-ADB-005`
- `FR-ADB-006`
- `FR-SEC-004`
- `TR-ADB-001`
- `TR-ADB-002`
- `TR-ADB-003`
- `TR-ADB-004`
- `TR-ADB-005`
- `TR-ADB-006`
- `TR-ADB-007`
- `TR-SEC-015`

Tests/evidence:

- `TEST-ADB-001`
- `TEST-ADB-002`
- `TEST-ADB-003`
- `TEST-ADB-004`
- `TEST-ADB-005`
- `TEST-MANUAL-003`

## Iteration 6 - CI, Packaging, and Release

Requirements:

- `TR-PACK-001`
- `TR-PACK-004`
- `TR-CI-001`
- `TR-CI-002`
- `TR-CI-003`
- `TR-CI-004`
- `TR-CI-005`

Tests/evidence:

- `TEST-PACK-001`
- `TEST-PACK-002`
- `TEST-CI-001`
- `TEST-CI-002`
- package artifacts
- tagged release dry run before first public publish
