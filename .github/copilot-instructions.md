# Avalonia.RemoteControl Workspace Instructions

## Project State

This repository contains a .NET 10 / Avalonia 12 remote-control system with implementation evidence through the protocol/read-only inspection, remote actions, logging, desktop client/tool, host-side ADB client workflow, CI, and packaging slices. Technical Spike 0 for real Android app-side transport remains the next product-blocking proof.

Do not claim feature behavior beyond the current implemented APIs until the owning Byrd slice has tests and implementation.

## Planned Build Commands

```powershell
dotnet restore Avalonia.RemoteControl.slnx
dotnet build Avalonia.RemoteControl.slnx --configuration Release --no-restore
dotnet test Avalonia.RemoteControl.slnx --configuration Release --no-build
dotnet pack Avalonia.RemoteControl.slnx --configuration Release --no-build --output artifacts/packages
```

## Planned Target Stack

- .NET 10
- Avalonia 12
- gRPC for the desktop-facing protocol
- NuGet package for `Avalonia.RemoteControl.Server`
- .NET tool package for `Avalonia.RemoteControl.Tool`
- GitHub Actions and Azure Pipelines

## Engineering Rules

- Requirements and traceability must be updated before implementation.
- Public APIs require XML documentation.
- Tests precede implementation for each Byrd slice.
- Zero skipped tests are allowed in completed validation scopes.
- Treat the remote-control surface as security-sensitive.
- Keep Android/ADB behavior behind explicit requirements and proof spikes until transport feasibility is verified.
