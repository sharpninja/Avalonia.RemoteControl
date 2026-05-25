# Agent Instructions

## Session Start

1. Check whether `AGENTS-README-FIRST.yaml` exists in the repo root.
2. If the marker exists, follow it for MCP trust, session log, TODO, and requirements tooling.
3. If the marker does not exist, do not fabricate MCP availability. Work from the local repository, document the missing marker as a foundation gap, and keep requirements/traceability files current.

On every subsequent user message:

1. Re-read this file when workspace rules may affect the request.
2. Complete the user's request.

## Rules

1. Keep this file focused on durable workspace policy and conventions; avoid duplicating marker-file operational procedures once a marker exists.
2. Use helper modules or MCP tools for session log, TODO, and requirements operations when this workspace is registered and trusted. Do not make raw API calls when a supported helper/tool exists.
3. Persist requirements, traceability, and design decisions immediately after each meaningful change. Do not defer requirements documentation to the end of an implementation slice.
4. Do not fabricate information. If you made a mistake, acknowledge it. Distinguish facts from speculation.
5. Prioritize correctness over speed. Do not ship code you have not verified compiles and is logically sound.
6. Public APIs must be documented.
7. Follow DRY, SOLID, and existing project conventions as they are established.
8. Use `pwsh`/PowerShell commands in this Windows workspace unless a tool or script requires another shell.
9. Keep external source references in the relevant requirements or architecture document when they inform requirements.
10. Treat this project as security-sensitive because it exposes a remote debugging and mutation surface.

## Byrd Development Process

The local process summary is `docs/Development-Process.md`. The canonical source used to initialize it is `F:\GitHub\McpServer\docs\Development-Process-draft-v3.md`.

Key gates:

- Requirements discovery and documentation happen before implementation.
- Public interfaces are documented before implementation code.
- Each implementation slice starts with tests.
- To leave a Byrd implementation slice, the entire unit test suite for the current iteration and previous completed iterations must pass.
- Skipped tests are not passing tests. A validation gate requires zero failures and zero skips in the executed scope.
- Deferred work belongs in requirements/TODO/execution state, not in skipped test placeholders.

## Where Things Live

- `AGENTS.md` - durable workspace policy and current agent instructions.
- `AGENTS-README-FIRST.yaml` - optional MCP marker; not present at initialization time.
- `.github/copilot-instructions.md` - build/test commands, architecture overview, coding conventions.
- `docs/Development-Process.md` - local Byrd process summary.
- `docs/requirements/` - product, functional, technical, testing, and traceability requirements.
- `docs/architecture/` - architecture decisions and subsystem designs.

## Current Workspace State

This repository is in Byrd Iteration 0 foundation implementation. It now contains a .NET 10 solution skeleton, package metadata, CI scaffolding, requirements/architecture docs, a sample Avalonia app shell, and an initial test project.

Configured remotes:

- `origin` - Azure DevOps private project/repo.
- `github` - public GitHub repository `sharpninja/Avalonia.RemoteControl`.

Current validation commands:

```powershell
dotnet restore Avalonia.RemoteControl.slnx
dotnet build Avalonia.RemoteControl.slnx --configuration Release --no-restore
dotnet test Avalonia.RemoteControl.slnx --configuration Release --no-build
dotnet pack Avalonia.RemoteControl.slnx --configuration Release --no-build --output artifacts/packages
```

There is not yet an MCP marker. Do not claim MCP session/TODO integration exists until `AGENTS-README-FIRST.yaml` or another supported workspace registration artifact is present and verified.

## Planned Product

Avalonia.RemoteControl is a .NET 10 / Avalonia 12 debugging and control system for inspecting and manipulating a running Avalonia application through a desktop client.

Planned packages:

- `Avalonia.RemoteControl.Server` - embeddable server SDK package.
- `Avalonia.RemoteControl.Tool` - .NET tool package exposing `avalonia-remote`.

Planned client command:

- `avalonia-remote`

## Requirements Tracking

When requirements are discovered or changed:

1. Update the relevant file under `docs/requirements/`.
2. Update `docs/requirements/traceability-matrix.md`.
3. Link requirements to testing evidence before implementation starts.
4. Keep security-sensitive requirements split between `FR-SEC-*` and `TR-SEC-*`; do not create standalone `SR-*` requirements.

## Design Decision Logging

When a design decision is made:

1. Record the decision in the affected requirements or architecture file.
2. Include alternatives considered, rationale, and affected requirement IDs.
3. If MCP session logging becomes available, also record the decision through the supported session-log path.

## Agent Conduct

You represent the workspace owner. Your work directly reflects the owner's professional reputation.

- Complete the user's request.
- Do not fabricate information, capabilities, or results.
- Distinguish facts from speculation.
- Acknowledge mistakes immediately and correct them.
- Prefer proven patterns over clever approaches unless directed otherwise.
- Leave unrelated user changes alone.

## Response Formatting

- Use concise bullets or short paragraphs.
- Do not use table-style output unless the user explicitly asks for it.
