# Project Sessions And Replay

The client project system is a user-scoped persistence layer for debugging work. It is intentionally client-side; the debuggee server does not need to know about projects.

## Scope

The first implementation stores:

- app connection profiles;
- project sessions;
- structured remote log history and client status rows;
- replayable interaction records;
- tree snapshot replay artifacts;
- replay results and per-step tree diffs.

The initial replay diff compares serialized tree snapshots. Frame or screenshot diffs are a future extension.

## Storage

`FileRemoteControlProjectStore` writes versioned JSON project files under the current user's application data folder. The document root is `RemoteControlProjectDocument` with schema version `1`.

Project records use stable identifiers for projects, app profiles, sessions, steps, and artifacts. The store is additive so future schema versions can keep old fields readable.

## Recording

The desktop client starts a project session after a successful connection. It records the normalized connection profile, streamed log rows, and command interactions. Click, focus, and property interactions capture before/after tree snapshot artifacts when a current snapshot is available. Live view input is recorded as replayable input events.

Sensitive replay payloads are not logger diagnostics. Text input and sensitive property values are marked in `SensitiveFields` so display surfaces can avoid echoing them while the local replay record remains capable of reproducing user-approved flows.

## Replay

`RemoteControlSessionReplayService` replays a recorded session through `IRemoteControlReplayTarget`. The shipped `RemoteControlDesktopReplayTarget` adapts a live `RemoteControlDesktopSession`.

Each replay step:

1. optionally honors recorded timing;
2. invokes the recorded command or input batch;
3. captures the current tree snapshot;
4. compares that replay snapshot to the original after-step artifact;
5. returns a `RemoteControlReplayStepResult`.

## Diffing

`RemoteControlReplayDiffService` compares nodes by stable node ID and reports:

- `Added`: node exists only in the replayed state;
- `Removed`: node exists only in the original state;
- `Changed`: node exists in both states but captured fields differ;
- `Unchanged`: node exists in both states with no captured differences.

Changed fields include core node identity, bounds, state flags, classes, and captured property values.
