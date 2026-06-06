# Troubleshooting

For setting-by-setting explanations, see [Settings Guide](Settings-Guide).

## Tool Command Not Found

Install or update the .NET tool:

```powershell
dotnet tool install --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.7.3
dotnet tool update --global SharpNinja.Avalonia.RemoteControl.Tool --version 0.7.3
```

Make sure the .NET global tools directory is on `PATH`.

## Server Does Not Start

Check startup validation:

- `IsEnabled` must be true.
- `AuthenticationToken` is required when authentication is enabled.
- Non-loopback listeners need `TlsCertificatePath`.
- Cleartext without TLS is accepted only for loopback or explicit ADB tunnel sessions.

Check debuggee logs for:

```text
Avalonia.RemoteControl startup validation failed
```

## Client Cannot Connect

Verify:

- Endpoint host and port.
- Token matches the debuggee.
- The debuggee process is still running.
- The server logged its bound address.
- TLS certificate trust is configured for network endpoints.

For local cleartext HTTP/2, use `http://127.0.0.1:<port>`.

## Authentication Fails

Bearer token authentication is required for all transports. Recreate the token, restart the debuggee, and reconnect with the same value.

Do not include `Bearer ` in the token field unless a specific API asks for a full authorization header.

## Tree Is Empty

The root provider probably returned null. Confirm that your `IRemoteControlRootProvider` is registered after `AddAvaloniaRemoteControl` and returns the current `Window`, `TopLevel`, or root `Control`.

## Property Edit Is Denied

Mutation can fail when:

- The property is not in `AllowedMutableProperties`.
- The property is sensitive by name.
- The property is not public and settable.
- The value cannot be converted to the target type.
- The selected node is stale.

Refresh the tree after stale-node errors.

## Click Or Focus Is Denied

Remote actions require:

```csharp
options.AllowRemoteActions = true;
```

The selected control must also still exist and support the requested action.

## Live View Is Blank Or Falls Back To Tree Mode

Live screenshots require:

```csharp
options.AllowRemoteFrames = true;
```

Remote input requires both:

```csharp
options.AllowRemoteActions = true;
options.AllowRemoteInput = true;
```

If frame streaming is disabled, the client can still use tree replica mode when tree streaming is available. If input is disabled, the live window remains view-only.

## Logs Are Missing

Confirm that the debuggee uses `Microsoft.Extensions.Logging` and that remote-control services are registered in the same service provider as the app logging pipeline.

The desktop client defaults to Warning verbosity. Switch Verbosity to Information or Debug when you need routine app diagnostics or remote-control protocol diagnostics.

If log volume is high, check dropped-message counts and increase:

```csharp
options.LogBufferCapacity = 4096;
```

## ADB Device Not Listed

Run:

```powershell
adb devices -l
```

Fix the Android SDK path, emulator/device state, or device authorization before using `avalonia-remote adb`.

## ADB Package Marker Cannot Be Read

Package marker discovery uses:

```text
adb shell run-as <package> cat files/avalonia-remote-control.json
```

Common causes:

- App is not debuggable.
- Package name is wrong.
- App did not start the bridge listener.
- App did not write the marker file.
- Device has stale app data from a previous build.

Fallback:

```powershell
avalonia-remote adb connect --serial <serial> --device-port 47100 --token <token>
```

## ADB Connect Says Package Is Not Running

Package marker files can remain in app-private storage after the app process exits. The marker proves that a previous app run wrote endpoint metadata; it does not prove that the bridge listener is alive now.

Check the app process:

```powershell
adb -s <serial> shell pidof <package>
```

If there is no process ID, launch the app on the device, wait for startup, then rerun `avalonia-remote adb connect`. If the process is running but the bridge closes before a response, remove the forward, restart the app, and connect again.

## Codex MCP Cannot See The App

The embedded Codex workflow uses the running desktop client as the MCP host. Reconnect the desktop client first, then stop and restart the terminal with Codex MCP.

Check:

- The desktop client is connected before Codex MCP starts.
- The terminal Working Dir is the directory you intended.
- Codex has an `avalonia_remote_control` MCP server for this terminal session.
- The debuggee allows the action or property mutation you asked Codex to perform.
- Codex refreshed the snapshot after each mutation instead of reusing stale node IDs.

## NuGet Restore Cannot Resolve Packages

Use the `SharpNinja.Avalonia.RemoteControl.*` package IDs. The older `Avalonia.RemoteControl.*` IDs are not the public package names.

```powershell
dotnet add package SharpNinja.Avalonia.RemoteControl.Server --version 0.7.3
dotnet add package SharpNinja.Avalonia.RemoteControl.Runtime --version 0.7.3
```
