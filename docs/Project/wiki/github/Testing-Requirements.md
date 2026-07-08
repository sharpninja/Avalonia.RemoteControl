# Testing Requirements (MCP Server)

## TEST-ADB

### TEST-ADB-001

Client lists connected ADB devices/emulators.


### TEST-ADB-002

Client selects a specific serial when multiple devices are present.


### TEST-ADB-003

Client creates an ADB forward to the selected device.


### TEST-ADB-004

Client connects through the forwarded localhost port.


### TEST-ADB-005

Client cleans up forwarding on disconnect by default.


### TEST-ADB-006

Android emulator smoke validates the selected app-side transport.


### TEST-ADB-007

Android bridge acceptance records build/install/launch, marker discovery, authenticated capabilities, tree snapshot, and ADB forward cleanup evidence on a real emulator or device.


### TEST-ADB-008

Unit tests verify versioned Android marker protocol parsing and fail-closed handling for unsupported marker transports.


### TEST-ADB-009

Unit tests verify the arc-protobuf-v1 bridge envelope encodes and decodes length-prefixed protobuf request and response frames and rejects oversized frames.


### TEST-ADB-010

Unit tests and build checks verify the host-independent runtime builds for net10.0-android, dispatches bridge requests with bearer authentication, and lets the host client probe marker-discovered arc-protobuf-v1 endpoints.


### TEST-ADB-011

Unit tests and build checks verify the app-side bridge listener accepts authenticated length-prefixed protobuf requests, writes Android marker metadata, and can be referenced by the Android probe sample without ASP.NET Core/Kestrel dependencies.


### TEST-ADB-013

Tests must verify bridge streaming for tree and frame streams, bridge cancellation by socket close, and unsupported capability handling.


### TEST-ADB-014

Unit tests verify package-marker adb connect fails before forwarding when pidof shows the package is stopped, and verify an Android bridge connection that closes before a response frame is reported as a clean InvalidOperationException.


### TEST-ADB-015

Unit tests verify the reusable ADB desktop and CLI connection workflow can launch a stopped package, wait for it to run, discover marker metadata, create a forward, probe capabilities, and save a transport-aware profile without exposing tokens in status output.


### TEST-ADB-016

Unit tests verify an explicit ADB bridge connection can create a selected-device host-to-device forward, probe the forwarded endpoint, and save a desktop profile with serial, host port, device port, adb mode, and arc-protobuf-v1 transport metadata.



## TEST-ANDROID

### TEST-ANDROID-001

Automated tests must verify MCP initialize/list/call behavior for the new Android tool names and validate that tool outputs are structured and sanitized.


### TEST-ANDROID-002

Automated tests must verify Android device, app, logcat, screenshot, UI tree, input, AVD listing, and emulator launch command construction using fake command runners without requiring a real device.


### TEST-ANDROID-003

Manual or automated validation must launch the Pixel 6 AVD through the new integrated tooling and use the same tool surface to inspect, launch, and interact with an Android target.


### TEST-ANDROID-004

Automated tests must verify that idle Android log polling timeouts are reported as recoverable liveness issues and that the client can restart log watching without misclassifying the target app as crashed.



## TEST-AVA

### TEST-AVA-001

Headless Avalonia tests prove dispatcher-safe tree capture.


### TEST-AVA-002

Headless Avalonia tests prove live update signaling after layout/state changes.


### TEST-AVA-003

Headless Avalonia tests prove click/focus invocation for supported controls.


### TEST-AVA-004

Headless Avalonia tests prove safe property mutation on sample controls.


### TEST-AVA-005

Headless Avalonia tests must verify dispatcher-safe frame capture, max size rejection, and frame stream cancellation.


### TEST-AVA-006

Headless Avalonia tests must verify pointer, wheel, keyboard, and text input dispatch to the remote root or focused control.


### TEST-AVA-007

Validation must cover that frame capture, tree snapshots, and input dispatch use the TopLevel surface when the registered root provider returns a child view.



## TEST-CI

### TEST-CI-001

GitHub Actions runs restore/build/test/pack.


### TEST-CI-002

Azure Pipelines runs restore/build/test/pack.



## TEST-CLIENT

### TEST-CLIENT-001

Tests must verify live-view coordinate mapping, frame updates, input batching, and tree-replica model updates.


### TEST-CLIENT-002

Client tests verify the supported log verbosity options and selected minimum level mapping for Debug, Information, Warning, and Error.


### TEST-CLIENT-003

Unit tests verify live-view hit testing chooses the deepest visible node whose absolute bounds contain the clicked root-relative point and ignores invisible or out-of-bounds nodes.


### TEST-CLIENT-004

Unit tests verify the reusable live-view surface keeps the existing hit-test selection behavior while enabling both generic floating tool-window and direct right-side dock-tab hosting paths without nested live-view dock chrome.


### TEST-CLIENT-005

Build validation verifies the Avalonia XAML styles for Visual Studio-like dockable panels compile, and tests continue to verify the dockable log/live-view models preserve existing behavior.


### TEST-CLIENT-006

Unit tests verify project documents preserve app connection profiles, sessions, log history, replay steps, and artifact references across save/load round trips.


### TEST-CLIENT-007

Unit tests verify replay diff generation reports added, removed, changed, and unchanged control-tree state for each replayed interaction step.


### TEST-CLIENT-008

Unit tests verify replay records can mark sensitive payload fields and do not format bearer tokens or typed text into diagnostic log messages.


### TEST-CLIENT-009

Build and unit validation verify the live-view float command opens a generic floating tool window and the right-side Live View tab hosts constrained live-view content directly without nested tool-window chrome or toolbar overflow.


### TEST-CLIENT-010

Unit tests verify project documents persist and restore client layout state including window dimensions, splitter sizes, selected panel tab, log pop-out state, and live-view dock state.


### TEST-CLIENT-011

Build and unit validation must verify the dock chrome model persists panel state and that the Avalonia XAML for icon commands, draggable headers, floating windows, dock-back commands, and hidden or auto-hide states compiles without regressing existing log and live-view behavior.


### TEST-CLIENT-012

Build and unit validation must verify Iciclecreek terminal package wiring, terminal panel XAML compilation, view-model defaults, and non-blocking terminal lifecycle state.


### TEST-CLIENT-013

Build and unit validation must verify the main shell dock layout exposes west, east, south, and default workspace regions and that the terminal panel is hosted in the default undeclared workspace surface.


### TEST-CLIENT-014

Unit tests verify both the legacy diagnostic stdio server and the in-process Streamable HTTP MCP endpoint handle initialize, tool listing, and tool call JSON-RPC messages without emitting non-MCP payloads.


### TEST-CLIENT-015

Unit tests verify the embedded terminal Codex MCP preset registers the running app loopback MCP URL, seeds guidance for using capabilities, snapshots, focus, click, and property tools with tree-first node discovery instead of screenshots, uses the captured tool startup working directory instead of a later process current directory, returns equivalent MCP initialize instructions, and does not pass remote endpoint, transport, bearer token, environment variable names, profile arguments, or avalonia-remote mcp child-process commands.


### TEST-CLIENT-016

Unit tests verify selected-node properties are exposed as property-grid descriptors, readonly and redacted properties cannot be edited, duplicate property names get stable descriptor names, edit events update rows and request remote mutation, and clearing the selected node clears property-grid state.


### TEST-CLIENT-017

Unit tests verify enum-valued remote properties expose an enum descriptor type with the reported value names and that selecting an enum value produces the remote string value used by SetPropertyAsync.


### TEST-CLIENT-018

Tests must prove the server exposes the configured audit identity through capabilities, bridge and gRPC clients preserve it, and the desktop shell stores and resets the displayed identity.



## TEST-DOC

### TEST-DOC-001

User documentation is linked from the README and includes current-version tool install commands, server package install commands, a local desktop UI quickstart, an Android desktop UI quickstart, Codex MCP usage guidance, security guidance, settings reference, and troubleshooting guidance.



## TEST-DOCKING

### TEST-DOCKING-001

xunit.v3 tests covering the factory layout structure/proportions/capabilities, view-locator resolution and panel DataContext binding, factory show/hide ops and VM events, and store save/load round-trip; plus a reflection test asserting legacy dock types are removed.

**Acceptance Criteria:**
- [ ] RemoteControlDock* test classes pass under Avalonia.Headless.XUnit.
- [ ] NoLegacyDockTypesRemain confirms the legacy dock types are gone.


## TEST-GATE

### TEST-GATE-001

A completed Byrd slice must run the current and prior completed slice tests with zero failures.


### TEST-GATE-002

A completed Byrd slice must have zero skipped tests in the executed validation scope.


### TEST-GATE-003

Deferred work is tracked in requirements/TODO state, not as skipped tests.



## TEST-GRPC

### TEST-GRPC-001

Unauthenticated RPCs fail.


### TEST-GRPC-002

Invalid tokens fail.


### TEST-GRPC-003

Authenticated snapshot requests succeed.


### TEST-GRPC-004

Tree stream sends initial state and subsequent updates.


### TEST-GRPC-005

Log stream sends ordered log messages and dropped-message metadata.


### TEST-GRPC-006

Stale node IDs return recoverable errors.


### TEST-GRPC-007

Canceled streams release resources.


### TEST-GRPC-008

Tests must verify authenticated gRPC frame streaming, cancellation, and policy rejection.


### TEST-GRPC-009

Tests must verify SendInput policy rejection, enabled dispatch, pointer drag state, keyboard/text routing, and sanitized audit logs.



## TEST-LIVE-VIEW

### TEST-LIVE-VIEW-008

Unit tests verify live-view hit testing chooses the deepest visible node whose absolute bounds contain the clicked root-relative point and ignores invisible or out-of-bounds nodes.



## TEST-LOG

### TEST-LOG-001

Unit tests verify Debug ILogger messages for incoming client operations and outgoing runtime responses or stream updates, including the non-recursive WatchLogs lifecycle behavior.


### TEST-LOG-002

Unit tests verify the Android bridge TCP transport writes Debug diagnostics for bridge request and response frame lifecycle and does not emit per-entry response-frame logs for WatchLogs streams.


### TEST-LOG-003

Unit tests verify service registration allows the remote-control ILoggerProvider to capture Debug entries while leaving other provider defaults unchanged.


### TEST-LOG-004

Unit tests verify the desktop client log default and row formatter so Warning is requested by default while Debug remains selectable, and visible rows include dropped-count and diagnostic metadata.


### TEST-LOG-005

Unit tests verify the pop-out log window model shares the same live log rows as the main log panel without starting an additional log stream.


### TEST-LOG-006

Unit or UI-model tests verify that popping out logs removes the embedded main-window log list, the pop-out exposes a dock command, and docking restores the embedded list without creating a duplicate log stream.


### TEST-LOG-007

Unit tests verify the shared log view model exposes supported verbosity options, tracks selected verbosity changes, and notifies the client UI so embedded and pop-out selectors stay synchronized.



## TEST-MANUAL

### TEST-MANUAL-001

Launch sample debuggee and connect from client over loopback.


### TEST-MANUAL-002

Connect over TLS/token to a non-loopback endpoint.


### TEST-MANUAL-003

Connect to Android emulator/device through ADB without manual port-forward commands.


### TEST-MANUAL-004

View live tree updates.


### TEST-MANUAL-005

Stream logs.


### TEST-MANUAL-006

Invoke a remote click.


### TEST-MANUAL-007

Edit an allowed property and observe the app/client update.


### TEST-MANUAL-008

Verify mutation audit trail.



## TEST-MCP

### TEST-MCP-001

Deterministic tests must build aiUnit evidence from the avalonia-remote in-process MCP host, verify the tool catalog and JSON-RPC calls, generate wireframe and screenshot image attachments, and validate a strict aiUnit JSON review schema for MCP interaction plus wireframe-vs-screenshot behavior.


### TEST-MCP-002

aiUnit tests must continue to verify that appsettings.aiunit.json uses the installed Codex CLI strategy so opt-in live aiUnit review can run through the local codex command.



## TEST-PACK

### TEST-PACK-001

`dotnet pack --no-build` produces the server NuGet package.


### TEST-PACK-002

`dotnet pack --no-build` produces the tool package.


### TEST-PACK-003

The tool can be installed from local artifacts.


### TEST-PACK-004

`avalonia-remote --help` works after local install.


### TEST-PACK-005

dotnet pack --no-build produces the Avalonia.RemoteControl.Runtime NuGet package.



## TEST-SEC

### TEST-SEC-001

Non-loopback cleartext startup fails.


### TEST-SEC-002

Loopback explicit config succeeds.


### TEST-SEC-003

ADB tunnel mode still requires token authentication.


### TEST-SEC-004

Sensitive properties and log fields are redacted.


### TEST-SEC-005

Blocked mutations are rejected and audited.


### TEST-SEC-006

Authorized mutations are audited.


### TEST-SEC-007

Failed commands do not leak raw exception dumps.


### TEST-SEC-008

Unit or integration tests verify certificate inspection, accepted fingerprint trust, rejected fingerprint mismatch, and profile forget behavior for saved certificate trust.



## TEST-UNIT

### TEST-UNIT-001

Snapshot mapping includes node identity, hierarchy, metadata, bounds, state, and property metadata.


### TEST-UNIT-002

Property redaction blocks sensitive names by default.


### TEST-UNIT-003

Property conversion accepts supported scalar/common Avalonia value types and rejects unsupported types.


### TEST-UNIT-004

Mutation policy denies by default and allows only configured members/types.


### TEST-UNIT-005

Log buffering reports dropped messages.


### TEST-UNIT-006

Auth/TLS option validation enforces safe defaults.
