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

- `Avalonia.RemoteControl.Server.<version>.nupkg`
- `Avalonia.RemoteControl.Server.<version>.snupkg`
- `Avalonia.RemoteControl.Tool.<version>.nupkg`
- `Avalonia.RemoteControl.Tool.<version>.snupkg`

## Publish Gates

GitHub Actions and Azure Pipelines both run:

```powershell
dotnet restore Avalonia.RemoteControl.slnx
dotnet build Avalonia.RemoteControl.slnx --configuration Release --no-restore
dotnet test Avalonia.RemoteControl.slnx --configuration Release --no-build
dotnet pack Avalonia.RemoteControl.slnx --configuration Release --no-build --output <artifact-dir>
```

Tagged `v*` builds publish only when package secrets are configured.

Duplicate publish prevention uses `dotnet nuget push --skip-duplicate` in both GitHub and Azure release paths.
