# Android Bridge Transport

## Status

Implemented through app-side listener, Android probe build evidence, and physical-device ADB acceptance for `ADB-BRIDGE-001`.

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
- `arc-protobuf-v1`: Android-compatible bridge protocol.

The client must reject unknown marker protocols before creating an ADB forward. The current client supports `grpc` and `arc-protobuf-v1` for authenticated unary bridge operations.

## Bridge Shape

The Android app-side bridge is a small loopback TCP listener using an explicit length-prefixed protobuf request/response envelope. It avoids `Microsoft.AspNetCore.App`, Kestrel, and `Grpc.AspNetCore` in the Android target.

The protocol project defines `BridgeRequest`, `BridgeResponse`, `BridgeMethod`, and `BridgeStatus` protobuf contract types plus a host-agnostic frame codec for the length-prefixed envelope. This establishes the wire shape before Android app-side implementation starts.

The bridge API surface should mirror the product operations instead of exposing arbitrary object access:

- get capabilities
- get snapshot
- watch tree
- invoke click
- set property
- watch logs

The implementation reuses dispatcher, snapshot, mutation, action, logging, redaction, and authorization services through `Avalonia.RemoteControl.Runtime`. That project targets `net10.0` and `net10.0-android`; it does not reference `Microsoft.AspNetCore.App`, Kestrel, or `Grpc.AspNetCore`.

The desktop tool should select the transport from the marker:

- `grpc` uses the existing `GrpcRemoteControlProbe` and gRPC desktop session.
- `arc-protobuf-v1` uses the bridge client adapter behind the same desktop session/probe workflow for capabilities, snapshots, click, focus, and property mutation.

When `avalonia-remote adb connect --keep-forward` succeeds, the CLI persists the forwarded endpoint, token, and selected transport protocol into the desktop client's user-scoped default profile. This lets the desktop UI reopen Android bridge sessions with `arc-protobuf-v1` instead of falling back to gRPC.

Bridge tree/log streaming remains future work. The Android app-side listener starts as a loopback-only TCP listener, dispatches authenticated unary requests through the runtime bridge handler, publishes package-private marker metadata, and stops with the debuggee lifecycle. The Android probe sample writes a per-process debug token into package-private marker JSON, so normal client discovery still uses `adb shell run-as` and does not log the token. If an external desktop-facing gRPC endpoint is still required for Android sessions, add a host-side localhost gRPC proxy in a later slice after the Android bridge proof passes.

## Evidence

- `RemoteControlBridgeTcpListenerTests` covers loopback binding, authenticated capabilities over TCP, snapshot response transport, marker creation, and package-private marker JSON writing.
- `samples/Avalonia.RemoteControl.AndroidProbe.Android` builds for `net10.0-android` and references `Avalonia.RemoteControl.Runtime`, not `Avalonia.RemoteControl.Server`.
- The Android probe uses `AvaloniaAndroidApplication<App>` plus non-generic `AvaloniaMainActivity`, then starts `RemoteControlBridgeTcpListener` from the activity lifecycle.
- Physical Android device `ZD222QH58Q` installed and launched the probe, exposed the marker through `adb shell run-as`, completed `avalonia-remote adb connect --keep-forward`, returned a 31-node snapshot over `arc-protobuf-v1`, and cleaned up the ADB forward.

## Alternatives

- Direct ASP.NET Core/Kestrel gRPC in the Android app: rejected by Technical Spike 0.
- Defer Android support: rejected because ADB connectivity is an MVP capability.
- Use marker protocol negotiation and fail closed until a protocol is implemented: selected because it preserves the current ADB UX and gives the bridge proof a testable contract.

## Requirements

- `FR-ADB-004`
- `FR-CLIENT-007`
- `FR-SEC-004`
- `TR-ADB-CONNECTIVITY-004`
- `TR-ADB-CONNECTIVITY-006`
- `TR-ADB-CONNECTIVITY-009`
- `TR-ADB-CONNECTIVITY-010`
- `TR-ADB-CONNECTIVITY-011`
- `TR-ADB-CONNECTIVITY-012`
- `TR-ADB-CONNECTIVITY-013`
- `TR-ADB-CONNECTIVITY-014`
- `TR-ADB-CONNECTIVITY-015`
- `TR-ADB-CONNECTIVITY-016`
- `TR-SEC-SECURITY-002`
- `TR-SEC-SECURITY-015`
- `TEST-ADB-008`
- `TEST-ADB-009`
- `TEST-ADB-010`
- `TEST-ADB-011`
- `TEST-ADB-012`
