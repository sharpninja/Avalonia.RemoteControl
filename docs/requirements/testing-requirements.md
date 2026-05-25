# Testing Requirements

## General Gate

- `TEST-GATE-001`: A completed Byrd slice must run the current and prior completed slice tests with zero failures.
- `TEST-GATE-002`: A completed Byrd slice must have zero skipped tests in the executed validation scope.
- `TEST-GATE-003`: Deferred work is tracked in requirements/TODO state, not as skipped tests.

## Unit Tests

- `TEST-UNIT-001`: Snapshot mapping includes node identity, hierarchy, metadata, bounds, state, and property metadata.
- `TEST-UNIT-002`: Property redaction blocks sensitive names by default.
- `TEST-UNIT-003`: Property conversion accepts supported scalar/common Avalonia value types and rejects unsupported types.
- `TEST-UNIT-004`: Mutation policy denies by default and allows only configured members/types.
- `TEST-UNIT-005`: Log buffering reports dropped messages.
- `TEST-UNIT-006`: Auth/TLS option validation enforces safe defaults.

## Avalonia Tests

- `TEST-AVA-001`: Headless Avalonia tests prove dispatcher-safe tree capture.
- `TEST-AVA-002`: Headless Avalonia tests prove live update signaling after layout/state changes.
- `TEST-AVA-003`: Headless Avalonia tests prove click/focus invocation for supported controls.
- `TEST-AVA-004`: Headless Avalonia tests prove safe property mutation on sample controls.

## gRPC Integration Tests

- `TEST-GRPC-001`: Unauthenticated RPCs fail.
- `TEST-GRPC-002`: Invalid tokens fail.
- `TEST-GRPC-003`: Authenticated snapshot requests succeed.
- `TEST-GRPC-004`: Tree stream sends initial state and subsequent updates.
- `TEST-GRPC-005`: Log stream sends ordered log messages and dropped-message metadata.
- `TEST-GRPC-006`: Stale node IDs return recoverable errors.
- `TEST-GRPC-007`: Canceled streams release resources.

## Security Tests

- `TEST-SEC-001`: Non-loopback cleartext startup fails.
- `TEST-SEC-002`: Loopback explicit config succeeds.
- `TEST-SEC-003`: ADB tunnel mode still requires token authentication.
- `TEST-SEC-004`: Sensitive properties and log fields are redacted.
- `TEST-SEC-005`: Blocked mutations are rejected and audited.
- `TEST-SEC-006`: Authorized mutations are audited.
- `TEST-SEC-007`: Failed commands do not leak raw exception dumps.
- `TEST-SEC-008`: Unit or integration tests verify certificate inspection, accepted fingerprint trust, rejected fingerprint mismatch, and profile forget behavior for saved certificate trust.

## Android/ADB Tests

- `TEST-ADB-001`: Client lists connected ADB devices/emulators.
- `TEST-ADB-002`: Client selects a specific serial when multiple devices are present.
- `TEST-ADB-003`: Client creates an ADB forward to the selected device.
- `TEST-ADB-004`: Client connects through the forwarded localhost port.
- `TEST-ADB-005`: Client cleans up forwarding on disconnect by default.
- `TEST-ADB-006`: Android emulator smoke validates the selected app-side transport.
- `TEST-ADB-007`: Android bridge acceptance records build/install/launch, marker discovery, authenticated capabilities, tree snapshot, and ADB forward cleanup evidence on a real emulator or device.
- `TEST-ADB-008`: Unit tests verify versioned Android marker protocol parsing and fail-closed handling for unsupported marker transports.
- `TEST-ADB-009`: Unit tests verify the `arc-protobuf-v1` bridge envelope encodes and decodes length-prefixed protobuf request/response frames and rejects oversized frames.
- `TEST-ADB-010`: Unit tests and build checks verify the host-independent runtime builds for `net10.0-android`, dispatches bridge requests with bearer authentication, and lets the host client probe marker-discovered `arc-protobuf-v1` endpoints.
- `TEST-ADB-011`: Unit tests and build checks verify the app-side bridge listener accepts authenticated length-prefixed protobuf requests, writes Android marker metadata, and can be referenced by the Android probe sample without ASP.NET Core/Kestrel dependencies.

## Packaging and CI Tests

- `TEST-PACK-001`: `dotnet pack --no-build` produces the server NuGet package.
- `TEST-PACK-002`: `dotnet pack --no-build` produces the tool package.
- `TEST-PACK-003`: The tool can be installed from local artifacts.
- `TEST-PACK-004`: `avalonia-remote --help` works after local install.
- `TEST-PACK-005`: `dotnet pack --no-build` produces the runtime NuGet package.
- `TEST-PACK-006`: `dotnet pack --no-build` produces the protocol NuGet package used by SDK package dependencies.
- `TEST-CI-001`: GitHub Actions runs restore/build/test/pack.
- `TEST-CI-002`: Azure Pipelines runs restore/build/test/pack.

## Documentation Tests

- `TEST-DOC-001`: User docs are present and linked from the README, with commands for tool install, server package install, local connection, ADB connection, security, and troubleshooting.

## Manual Acceptance

- `TEST-MANUAL-001`: Launch sample debuggee and connect from client over loopback.
- `TEST-MANUAL-002`: Connect over TLS/token to a non-loopback endpoint.
- `TEST-MANUAL-003`: Connect to Android emulator/device through ADB without manual port-forward commands.
- `TEST-MANUAL-004`: View live tree updates.
- `TEST-MANUAL-005`: Stream logs.
- `TEST-MANUAL-006`: Invoke a remote click.
- `TEST-MANUAL-007`: Edit an allowed property and observe the app/client update.
- `TEST-MANUAL-008`: Verify mutation audit trail.
