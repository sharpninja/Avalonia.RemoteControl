# Release

## Source of Truth

The public GitHub repository `sharpninja/Avalonia.RemoteControl` is the package release source of truth.

Azure DevOps under `https://dev.azure.com/McpServer/Avalonia.RemoteControl` is a private validation and mirror target. Azure can publish only when a tagged build has a configured `NuGetApiKey` secret variable.

## Versioning

Package version is controlled by `Directory.Build.props`.

Tagged package releases use tags shaped as:

```text
v0.1.0
```

## Package Outputs

Release builds produce:

- `SharpNinja.Avalonia.RemoteControl.Server.<version>.nupkg`
- `SharpNinja.Avalonia.RemoteControl.Server.<version>.snupkg`
- `SharpNinja.Avalonia.RemoteControl.Protocol.<version>.nupkg`
- `SharpNinja.Avalonia.RemoteControl.Protocol.<version>.snupkg`
- `SharpNinja.Avalonia.RemoteControl.Runtime.<version>.nupkg`
- `SharpNinja.Avalonia.RemoteControl.Runtime.<version>.snupkg`
- `SharpNinja.Avalonia.RemoteControl.Tool.<version>.nupkg`
- `SharpNinja.Avalonia.RemoteControl.Tool.<version>.snupkg`

## Package ID Decision

Public NuGet package IDs use the `SharpNinja.Avalonia.RemoteControl.*` namespace. Earlier `Avalonia.RemoteControl.*` IDs conflicted with the verified `Avalonia` package prefix ownership model on nuget.org, so stable public releases are cut under the owner-controlled `SharpNinja` prefix.

## Publish Gates

GitHub Actions and Azure Pipelines both run:

```powershell
dotnet restore Avalonia.RemoteControl.slnx
dotnet build Avalonia.RemoteControl.slnx --configuration Release --no-restore
dotnet test Avalonia.RemoteControl.slnx --configuration Release --no-build
dotnet pack Avalonia.RemoteControl.slnx --configuration Release --no-build --output <artifact-dir>
```

Tagged `v*` builds publish only when package secrets are configured. Azure follows the aiUnit pattern: the secret pipeline variable is named `NuGetApiKey`, exposed to the publish step as `NUGET_API_KEY`, and read only through `$env:NUGET_API_KEY` inside the script. Package IDs use the `SharpNinja.Avalonia.RemoteControl.*` namespace.

Duplicate publish prevention uses `dotnet nuget push --skip-duplicate` in both GitHub and Azure release paths. Azure publishes the primary `.nupkg` packages and keeps `.snupkg` symbol packages in the build artifact.
