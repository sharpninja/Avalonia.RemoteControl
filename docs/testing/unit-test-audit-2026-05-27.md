# Unit Test Audit - 2026-05-27

## Scope

This audit reviewed the `tests/Avalonia.RemoteControl.Tests` unit/integration-style test project against the production surfaces in:

- `src/Avalonia.RemoteControl.Protocol`
- `src/Avalonia.RemoteControl.Runtime`
- `src/Avalonia.RemoteControl.Server`
- `src/Avalonia.RemoteControl.Client`
- `src/Avalonia.RemoteControl.Tool`

The immediate trigger was an embedded Codex MCP call to `avalonia_remote_get_capabilities` failing with an HTTP `text/plain` response containing `Cannot access a disposed object. Object name: 'JsonDocument'.`

## Current Coverage Inventory

| Area | Existing test files | Coverage summary |
| --- | --- | --- |
| Protocol and bridge framing | `RemoteControlBridgeProtocolTests`, `RemoteControlBridgeRequestHandlerTests`, `RemoteControlBridgeTcpListenerTests`, `RemoteControlLiveProtocolTests` | Length-prefixed bridge frames, oversized frame rejection, basic bridge request dispatch, bridge streams for frames/tree/logging lifecycle. |
| Runtime capabilities, snapshots, commands | `RemoteControlFoundationTests`, `RemoteControlReadOnlyInspectionTests`, `RemoteControlCommandTests`, `RemoteControlLiveRuntimeTests`, `RemoteControlTreeStreamTests` | Default security posture, snapshot metadata/redaction/stable IDs, click/focus/property/input policy gates, frame stream enablement, tree streaming. |
| Server hosting and security | `RemoteControlHostedServerTests`, `RemoteControlHostingTests`, `RemoteControlSecurityTests` | Bearer auth, TLS loopback/non-loopback startup policy, service registration, lifetime helper behavior. |
| Client transports and Android | `RemoteControlDesktopSessionTests`, `RemoteControlAdbClientTests`, `RemoteControlAndroidMcpToolTests` | gRPC/bridge session operations, certificate trust, ADB device parsing/forwarding/marker discovery/profile save, Android MCP command construction. |
| Live client model | `RemoteControlLiveClientTests` | Coordinate mapping, tree selection preservation, hit testing, older endpoint capability defaults. |
| Logging | `RemoteControlLoggingTests`, `RemoteControlProtocolEventLoggingTests` | Formatter output, shared log view model state, provider capture/redaction/buffer/keep-alive/filtering, runtime debug event logging. |
| MCP host | `RemoteControlMcpServerTests`, `AiUnitMcpServerIntegrationTests` | stdio/HTTP initialize/list/call, Android tool calls without remote session, explicit command-line configuration, aiUnit prompt/config/validator scaffolding. |
| Client project and UI shell | `RemoteControlProjectSystemTests`, `RemoteControlProfileStoreTests`, `RemoteControlTerminalPanelTests` | Profile/project persistence, replay diffing, layout defaults, terminal presets, dock layout measurement and hidden/auto-hidden panel sizing. |

## Fixed In This Pass

| Gap | Fix |
| --- | --- |
| JSON-RPC request IDs were held as `JsonElement` values past the lifetime of the parsed request document. | The MCP JSON-RPC handler now clones the id while the request `JsonDocument` is alive. |
| Runtime/session failures were classified as invalid params because `InvalidOperationException` was grouped with `ArgumentException`. | Only `ArgumentException` maps to JSON-RPC `-32602`; remote/session failures map to `-32000`. |
| A tool failure could escape the handler and be returned by the HTTP server as `text/plain`, which MCP clients reject before reading the JSON-RPC result. | The handler now converts identified tool failures into JSON-RPC responses. The HTTP fallback also uses `application/json`. |
| Notification-style calls without an id could surface as HTTP failures. | No-id failures are treated as notifications: diagnostics are written and HTTP returns `202 Accepted` without a response body. |

Regression tests added in `RemoteControlMcpServerTests`:

- `McpHttpErrorResponseUsesJsonRpcApplicationJsonShape`
- `McpStreamableHttpServerReturnsJsonRpcErrorWhenToolFails`
- `McpStreamableHttpServerReturnsInvalidParamsForToolArgumentFailures`
- `McpStreamableHttpServerSuppressesNotificationToolFailures`
- `McpStdioServerReturnsJsonRpcErrorWhenToolFails`
- `McpStdioServerReturnsInvalidParamsForToolArgumentFailures`
- `McpStdioServerSuppressesNotificationToolFailures`

## Implemented From Audit Findings

| Priority | Gap | Implementation |
| --- | --- | --- |
| P0 | MCP HTTP fallback could not be unit-tested directly. | Added `RemoteControlMcpHttpErrorResponse` as a pure response object and direct unit coverage for status, content type, JSON-RPC shape, and UTF-8 encoding. |
| P0 | Main window MCP host lifecycle was not unit-tested. | Added `RemoteControlMcpHostController` with injectable endpoint hosts, and tests for start, idempotent start, restart, terminal URL update, live option factory behavior, and disposal. |
| P0 | Live View dock startup/default state was only indirectly covered. | Added `RemoteControlToolShellViewModel` and tests for dock-on-connect default, frame streaming disabled by default, no startup live-view content, persisted dock preference, and reset after connection failure. |
| P1 | Logging end-to-end delivery into the client UI was not covered. | Added hosted server plus `RemoteControlDesktopSession` coverage that streams Warning-level logs through `RemoteLogViewModel` using the default Warning verbosity. |
| P1 | Android lifecycle/failure diagnostics were under-covered. | Added tests for APK install failure, package launch failure, and logcat fallback when package PID is missing. |
| P1 | Project replay/project file recovery did not cover corrupted or partial data. | Added `FileRemoteControlProjectStore` recovery for invalid JSON and normalization for partial documents with null layout/profile/session collections. |

## Remaining Gaps

### P0 - Should Be Closed Before Next Release Candidate

| Gap | Risk | Suggested tests |
| --- | --- | --- |
| None currently open from this audit pass. | P0 findings from this audit have direct automated coverage. | Continue treating new MCP transport failures and shell startup regressions as P0 until the UI shell is thinner. |

### P1 - High-Value Edge Coverage

| Gap | Risk | Suggested tests |
| --- | --- | --- |
| Bridge/gRPC stream cancellation coverage is present, but client UI recovery is not. | A closed Android bridge socket or log timeout can leave stale status or blocked controls. | Add client session/view-model tests for stream end, timeout, reconnect, and status reset. |
| Android Device Manager tests still do not cover emulator boot polling because no boot-wait workflow exists yet. | Pixel/AVD flows may fail after boot timeout with poor diagnostics once boot-wait is added. | Add a boot-wait workflow and fake runner tests for already-running AVD, boot complete, boot timeout, and emulator process start failure. |
| Docking behavior is measured but not exercised as commands. | Pin, auto-hide, float, dock, close, and layout restore can visually regress without failing tests. | Add command-level tests on panel view models or a docking state service for pin/unpin/floating/docked/hidden transitions and persisted layout round trip. |
| Project replay still lacks partial replay artifact tests below the top-level project/session collection level. | A partially written artifact can produce a replay diff failure later in the workflow. | Add normalization or validation for artifact tree snapshots with null node/property collections. |

### P2 - Broader Confidence Improvements

| Gap | Risk | Suggested tests |
| --- | --- | --- |
| Screenshot/live frame decode errors are not directly tested in the UI control layer. | Bad PNG bytes or mismatched render scale can leave the Live View blank. | Add tests for invalid frame payload, root DIP mismatch, fit/scroller coordinate mapping, and overlay selection after frame update. |
| aiUnit tests are mostly prompt/config unit checks, with live execution behind explicit enablement. | The advertised AI visual-review workflow can drift from the running tool. | Add a small deterministic local MCP host fixture for aiUnit to compare wireframe-vs-screenshot evidence without requiring a phone/emulator. |
| Requirements-to-tests traceability is manual. | FR/TR/TEST records can claim behavior that has no executable test. | Generate a requirements coverage report that maps TEST ids to test methods or explicit manual evidence. |
| Security tests cover startup/auth policy, but not sensitive data in MCP/terminal/project output. | Tokens or typed text can leak through command lines, session records, or debug logs. | Add assertions for MCP command seeding, project interaction summaries, terminal presets, and log redaction around tokens/password-like values. |

## Audit Conclusion

The suite is broad for core runtime and transport behavior. This implementation pass closed the P0 findings by extracting testable MCP host and shell startup seams, then added high-value P1 coverage for log delivery, Android failure diagnostics, and project-file recovery. The weakest remaining layer is docking command behavior: measurement is covered, but pin/float/dock/close command transitions should move behind a testable docking state service instead of relying on window event handlers.

## Validation

- `dotnet test tests/Avalonia.RemoteControl.Tests/Avalonia.RemoteControl.Tests.csproj --no-restore --filter RemoteControlMcpServerTests`: 15 passed, 0 failed, 0 skipped.
- `dotnet test Avalonia.RemoteControl.slnx --no-restore`: 157 passed, 0 failed, 0 skipped.
- `dotnet test tests/Avalonia.RemoteControl.Tests/Avalonia.RemoteControl.Tests.csproj --no-restore --filter 'RemoteControlMcpServerTests|RemoteControlTerminalPanelTests|RemoteControlProjectSystemTests|RemoteControlAndroidMcpToolTests|RemoteControlDesktopSessionTests'`: 56 passed, 0 failed, 0 skipped.
- `dotnet test Avalonia.RemoteControl.slnx --no-restore`: 167 passed, 0 failed, 0 skipped.
