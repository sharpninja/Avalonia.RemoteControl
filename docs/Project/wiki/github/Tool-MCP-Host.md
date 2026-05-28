# Tool MCP Host

The desktop tool must expose a real Model Context Protocol host so Codex can drive the connected remote-control session instead of only running beside it in an embedded terminal.

The running desktop tool hosts MCP directly in-process. It starts a loopback-only Streamable HTTP endpoint when the main window is created and exposes that endpoint to the embedded terminal view model. Codex connects to the already-running tool host through `mcp_servers.avalonia_remote_control.url`; it does not launch `avalonia-remote mcp`, receive a profile name, or receive remote endpoint/token/transport values. In Codex configuration the server key is `avalonia_remote_control`; the MCP initialize response reports server name `avalonia-remote-control` and title `Avalonia Remote Control`.

The embedded Codex preset passes a seed prompt after the MCP server override. The prompt tells Codex to use `avalonia_remote_get_capabilities` first, inspect the control tree with `avalonia_remote_get_snapshot`, find controls from node metadata, and avoid screenshots or pixel inspection as the primary control-selection mechanism. It also tells Codex to refresh the snapshot after mutations and to relocate controls instead of guessing stale node IDs.

The loopback endpoint uses a random path component, for example:

```text
http://127.0.0.1:{port}/mcp/{random-route-secret}
```

That URL is the only MCP-specific value passed to Codex. The remote app endpoint, transport protocol, bearer token, certificate path, and accepted fingerprint remain in the running GUI state and are read by the in-process MCP host when a tool call needs a `RemoteControlDesktopSession`.

The first in-process transport supports JSON-RPC requests over HTTP `POST`. It validates the random route path and rejects non-loopback `Origin` values before dispatching a message. Long-lived GET/SSE streams are intentionally not opened by this slice and return `405`.

The older `avalonia-remote mcp [stdio]` command remains a diagnostic transport for automation tests and manual protocol checks. It is not the embedded terminal integration path.

Exposed tools are intentionally narrow and map to existing remote-control client operations:

- `avalonia_remote_get_capabilities`: read endpoint capabilities before choosing interactions.
- `avalonia_remote_get_snapshot`: read the current Avalonia control tree, including node IDs and metadata used to choose targets.
- `avalonia_remote_invoke_click`: click a current node ID found from the latest snapshot.
- `avalonia_remote_focus`: focus a current node ID when focus affects the next interaction.
- `avalonia_remote_set_property`: set an approved public property, then refresh the snapshot to verify the result.

Remote mutation remains governed by the debuggee server's existing policy gates. The MCP host does not bypass authentication, transport selection, certificate trust, or server-side action/property policy.

External references:

- MCP transport specification 2025-06-18, Streamable HTTP and stdio transports: `https://modelcontextprotocol.io/specification/2025-06-18/basic/transports`
- MCP lifecycle specification 2025-06-18, initialize/initialized handshake: `https://modelcontextprotocol.io/specification/2025-06-18/basic/lifecycle`
- MCP tools specification 2025-06-18, `tools/list` and `tools/call`: `https://modelcontextprotocol.io/specification/2025-06-18/server/tools`
