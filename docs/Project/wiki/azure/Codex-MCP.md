# Codex MCP

The desktop client hosts an in-process Model Context Protocol server so Codex can inspect and control the connected Avalonia app through the same session the UI is using.

Codex does not launch `avalonia-remote mcp`, does not receive the remote endpoint or bearer token, and does not need environment variables for the app connection. The only MCP value passed to Codex is the running tool's loopback MCP URL. Endpoint, token, transport, certificate path, and accepted fingerprint stay in the desktop client's process state.

## Prerequisites

- `avalonia-remote` is installed and launches the desktop client.
- The `codex` CLI is installed and authenticated on the machine.
- The desktop client is connected to a debuggee app.
- The debuggee has enabled any mutation gates needed for the requested task.

## Start Codex From The Tool

1. Start `avalonia-remote` from the workspace directory you want Codex to use.
2. Connect to the local, TLS, or Android debuggee from the top connection bar.
3. Open the Workspace/Terminal tab in the main client area.
4. Leave Command as `pwsh.exe` unless you need a different shell.
5. Verify Working Dir. By default it is the current directory from when `avalonia-remote` was started.
6. Click Codex MCP.

The terminal launches Codex with a one-off MCP override for the running desktop tool. The MCP server key in Codex config is `avalonia_remote_control`. The MCP initialize response reports server name `avalonia-remote-control` and title `Avalonia Remote Control`.

## Tools Codex Receives

- `avalonia_remote_get_capabilities`: read the connected debuggee's supported protocol features and policy gates.
- `avalonia_remote_get_snapshot`: read the current Avalonia control tree, node IDs, bounds, state, names, classes, and safe public properties.
- `avalonia_remote_invoke_click`: click a node ID found from a current snapshot.
- `avalonia_remote_focus`: focus a node ID found from a current snapshot.
- `avalonia_remote_set_property`: set an approved public property on a node ID, subject to server policy.

The MCP host does not bypass authentication or server-side gates. If clicks, input, or property edits are disabled in the debuggee, the MCP tools return the same sanitized denial the desktop UI would show.

## How To Prompt Codex

Use prompts that tell Codex to work from the control tree first:

```text
Use the avalonia_remote_control MCP tools. Get capabilities, inspect the snapshot,
find the Settings tab by control tree metadata, click it, then refresh the snapshot
and report the selected node and visible state changes.
```

For property work:

```text
Use the control tree to find the selected TextBox. Inspect its properties, set the
Text property to "debug value" only if the server policy allows it, then refresh
the snapshot and report the before/after value.
```

Avoid screenshot-first prompts. Screenshots are useful for visual confirmation, but the MCP tools are designed around stable tree metadata and node IDs. After any mutation, ask Codex to refresh the snapshot before acting again because Avalonia controls can be recreated and old node IDs can become stale.

## Safety Notes

- Treat Codex MCP access as an operator action on the connected debuggee.
- Keep remote control disabled in production builds.
- Keep the bearer token out of prompts, logs, and source control.
- Enable `AllowRemoteActions`, `AllowRemoteFrames`, `AllowRemoteInput`, and mutable-property allow-lists only for controlled debugging sessions.
- The in-process MCP URL is loopback-only and contains a random route component; do not paste it into shared logs.

## Troubleshooting

- If Codex cannot see `avalonia_remote_control`, stop the terminal process and click Codex MCP again after the desktop client is fully connected.
- If tool calls return connection errors, reconnect the desktop client first. The MCP host reads the active GUI session settings.
- If clicks or property edits are denied, enable the corresponding debuggee gates and reconnect.
- If Codex starts in the wrong directory, stop the terminal, update Working Dir, and click Codex MCP again.
