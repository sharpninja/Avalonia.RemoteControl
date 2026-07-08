# Live Interactive Remote View

## Decision

The desktop tool provides a live remote view panel that can be docked in the right-side tool area or floated in a generic tool window. The panel has two render modes:

- screenshot mode, which streams PNG frames from the debuggee for visual fidelity
- tree replica mode, which draws the latest absolute control bounds and labels for structural debugging

Screenshot mode is the default. Tree data remains available for hit testing, overlays, and selection.

## Protocol Shape

The protocol adds `WatchFrames` and `SendInput` without removing existing RPCs. Frame stream messages carry PNG bytes plus pixel size, root DIP size, render scale, sequence, and timestamp. Input messages carry batched pointer, wheel, key, and text events in root-relative DIP coordinates.

Tree nodes keep existing local `bounds` and add root-relative `absolute_bounds` for live overlays and replica rendering.

## Runtime Shape

Frame capture runs on the Avalonia UI dispatcher through `RenderTargetBitmap`. Live frame streaming is disabled by default and requires `AllowRemoteFrames`.

Remote input dispatch runs on the Avalonia UI dispatcher. It requires both `AllowRemoteActions` and `AllowRemoteInput`. Pointer events maintain state for drag sequences; keyboard and text input target the focused element. Audit logs record sanitized input event counts and results, never typed text.

When an app supplies a child control as the remote root, runtime services normalize that control to its containing `TopLevel` before frame capture, tree snapshot traversal, or input dispatch. This keeps Android `ISingleViewApplicationLifetime.MainView` providers from omitting top-level background rendering and popup/flyout overlay layers.

## Transports

The gRPC transport exposes native streaming for frames and existing tree updates. The Android bridge keeps the length-prefixed protobuf envelope and supports long-lived streaming responses for `WatchTree` and `WatchFrames` over the forwarded TCP connection.

## Requirements

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
- `TEST-CLIENT-001`
- `TEST-GRPC-008`
- `TEST-GRPC-009`
- `TEST-ADB-013`
- `TEST-AVA-005`
- `TEST-AVA-006`
- `TEST-AVA-007`
