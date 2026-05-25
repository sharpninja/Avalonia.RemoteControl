# Avalonia.RemoteControl Workspace Instructions

## Project State

This repository is in Byrd Iteration 0 foundation implementation. It contains a .NET 10 solution skeleton, package metadata, CI scaffolding, requirements/architecture docs, a sample Avalonia app shell, and an initial test project.

Do not claim feature behavior beyond the current skeleton APIs until the owning Byrd slice has tests and implementation.

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
