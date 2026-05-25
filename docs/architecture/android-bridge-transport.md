# Android Bridge Transport

## Status

Proposed for `ADB-BRIDGE-001`.

## Context

Technical Spike 0 proved that the current ASP.NET Core/Kestrel gRPC host cannot be used directly inside a `net10.0-android` Avalonia app. The Android packaging failure is `NETSDK1082` because `Microsoft.AspNetCore.App` has no Android runtime pack.

The host-side ADB workflow remains valid: discover a debuggable package marker, create an `adb forward`, authenticate through a bearer token, probe the endpoint, and clean up the forward by default.

## Decision

Android app-side support will use a separate Android-compatible bridge protocol instead of embedding the ASP.NET Core server host in the Android debuggee.

The package-private marker at `files/avalonia-remote-control.json` is the protocol negotiation point. Missing protocol metadata is treated as legacy gRPC. New bridge markers must identify their app-side protocol explicitly.

Current marker fields:

```json
{
  "schemaVersion": "1",
  "devicePort": 47100,
  "token": "debug-session-token",
  "bridgeProtocol": "arc-protobuf-v1"
}
```

Supported marker protocol values:

- `grpc`: current desktop/server-capable gRPC endpoint.
- `arc-protobuf-v1`: reserved Android-compatible bridge protocol for the next proof slice.

Until the `arc-protobuf-v1` client adapter is implemented, the client must reject that marker before creating an ADB forward. This is a fail-closed behavior to avoid opening tunnels to an endpoint the tool cannot authenticate and validate correctly.

## Planned Bridge Shape

The Android app-side bridge should be a small loopback TCP listener using an explicit length-prefixed protobuf request/response envelope. It must avoid `Microsoft.AspNetCore.App`, Kestrel, and `Grpc.AspNetCore` in the Android target.

The protocol project now defines `BridgeRequest`, `BridgeResponse`, `BridgeMethod`, and `BridgeStatus` protobuf contract types plus a host-agnostic frame codec for the length-prefixed envelope. This establishes the wire shape before Android app-side implementation starts.

The bridge API surface should mirror the product operations instead of exposing arbitrary object access:

- get capabilities
- get snapshot
- watch tree
- invoke click
- set property
- watch logs

The implementation should reuse the dispatcher, snapshot, mutation, action, logging, redaction, and authorization services that do not require ASP.NET Core. Split those services into a host-agnostic runtime package before adding Android app-side code so the Android target does not inherit the `Microsoft.AspNetCore.App` framework reference.

The desktop tool should select the transport from the marker:

- `grpc` uses the existing `GrpcRemoteControlProbe` and gRPC desktop session.
- `arc-protobuf-v1` uses a future Android bridge adapter behind the same desktop UI workflows.

If an external desktop-facing gRPC endpoint is still required for Android sessions, add a host-side localhost gRPC proxy in a later slice after the Android bridge proof passes.

## Alternatives

- Direct ASP.NET Core/Kestrel gRPC in the Android app: rejected by Technical Spike 0.
- Defer Android support: rejected because ADB connectivity is an MVP capability.
- Use marker protocol negotiation and fail closed until the adapter exists: selected because it preserves the current ADB UX and gives the bridge proof a testable contract.

## Requirements

- `FR-ADB-004`
- `FR-SEC-004`
- `TR-ADB-CONNECTIVITY-004`
- `TR-ADB-CONNECTIVITY-006`
- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-010`
- `TR-ADB-CONNECTIVITY-011`
- `TR-ADB-CONNECTIVITY-012`
- `TR-ADB-CONNECTIVITY-013`
- `TR-ADB-CONNECTIVITY-014`
- `TR-SEC-SECURITY-002`
- `TR-SEC-SECURITY-015`
- `TEST-ADB-008`
- `TEST-ADB-009`
