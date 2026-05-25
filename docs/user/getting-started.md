# Getting Started

This guide connects a local Avalonia desktop app to the Avalonia.RemoteControl client.

## Prerequisites

- .NET 10 SDK
- Avalonia 12 app
- A debug build or another controlled environment where remote inspection is acceptable

## Install The Client Tool

```powershell
dotnet tool install --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.1.3
```

Update later with:

```powershell
dotnet tool update --global SharpNinja.Avalonia.RemoteControl.Tool
```

Verify the command:

```powershell
avalonia-remote --help
```

## Add The Server Package

For a desktop or server-capable Avalonia app, add:

```powershell
dotnet add package SharpNinja.Avalonia.RemoteControl.Server --version 0.1.3
```

The server package brings in the runtime and protocol packages.

## Enable Loopback Remote Control

Register the remote-control services in your app startup code. Keep the token out of source control.

```csharp
using System.Net;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;

public sealed class RemoteControlRootProvider : IRemoteControlRootProvider
{
    public Control? Root { get; set; }

    public Control? GetRootControl() => Root;
}
```

```csharp
private ServiceProvider? services;
private IDisposable? remoteControlLifetime;

public override void OnFrameworkInitializationCompleted()
{
    var rootProvider = new RemoteControlRootProvider();
    var serviceCollection = new ServiceCollection();

    serviceCollection.AddAvaloniaRemoteControl(options =>
    {
        options.IsEnabled = true;
        options.Host = IPAddress.Loopback;
        options.Port = 47100;
        options.AuthenticationToken = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_TOKEN");
        options.AllowRemoteActions = true;
        options.AllowRemoteFrames = true;
        options.AllowRemoteInput = true;
        options.AllowedMutableProperties.Add("Text");
    });

    serviceCollection.AddSingleton<IRemoteControlRootProvider>(rootProvider);

    services = serviceCollection.BuildServiceProvider();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var mainWindow = new MainWindow();
        rootProvider.Root = mainWindow;
        desktop.MainWindow = mainWindow;
        remoteControlLifetime = desktop.AttachAvaloniaRemoteControl(services);
    }

    base.OnFrameworkInitializationCompleted();
}
```

Important details:

- Register your `IRemoteControlRootProvider` after `AddAvaloniaRemoteControl` so it replaces the empty default provider.
- `AuthenticationToken` is required when remote control is enabled.
- Loopback cleartext is allowed by default; non-loopback listeners require TLS.
- Mutation still requires explicit property allow-list entries.
- Live screenshots require `AllowRemoteFrames`; live input requires both `AllowRemoteActions` and `AllowRemoteInput`.

## Run And Connect

Start the debuggee app with a token:

```powershell
$env:AVALONIA_REMOTE_TOKEN = "change-this-dev-token"
dotnet run --project path\to\YourApp.csproj
```

Launch the client:

```powershell
avalonia-remote
```

Use these connection values:

- Endpoint: `http://127.0.0.1:47100`
- Token: the value of `AVALONIA_REMOTE_TOKEN`
- Mode: Local

After connecting, the client can display the tree, selected-node properties, logs, and enabled actions. Click Live View to open the separate interactive remote UI window.
