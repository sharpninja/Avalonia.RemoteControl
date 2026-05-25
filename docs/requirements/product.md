# Product Requirements

## Product

Avalonia.RemoteControl is a debugging and control system for Avalonia 12 applications.

It lets a developer or QA engineer connect a desktop client to a running Avalonia app, inspect the live tree/state/logs, invoke interactions, and safely mutate approved public properties.

## Product Thesis

Avalonia developers need a debugger-grade inspection and interaction surface that works across desktop and Android targets without forcing each application to build custom debug UI.

The product is valuable only if it is easy to add to an app, easy to connect from the client, safe by default, and backed by strong traceability/tests.

## Users

- Avalonia application developers.
- QA engineers validating UI behavior.
- Support engineers reproducing diagnostic issues in controlled environments.
- Automation engineers building diagnostic workflows around a running app.

## Non-Goals

- This is not an end-user production control plane.
- This is not a replacement for platform accessibility tooling.
- This is not a general UI automation framework in v1.
- This does not expose private fields or arbitrary method invocation in v1.
- This does not weaken production app security defaults.

## MVP Capabilities

- Add an embeddable server SDK to an Avalonia app.
- Launch a desktop client through the `avalonia-remote` .NET tool.
- Authenticate to a running debuggee.
- Inspect the current Avalonia visual/control tree.
- Receive live tree and state updates.
- Inspect safe public properties and Avalonia state.
- Mutate approved public properties.
- Invoke basic remote interactions such as click and focus.
- Stream `ILogger` events.
- Connect to Android emulator/device apps through ADB forwarding without manual shell commands.
- Produce NuGet and .NET tool packages through CI.

## Package Targets

- `Avalonia.RemoteControl.Runtime` - host-independent runtime SDK for shared desktop and Android-compatible services.
- `Avalonia.RemoteControl.Server` - embeddable server SDK.
- `Avalonia.RemoteControl.Tool` - .NET tool launcher for the desktop client.

## External Hosting

- GitHub: `https://github.com/sharpninja/Avalonia.RemoteControl`
- Azure DevOps: `https://dev.azure.com/McpServer/Avalonia.RemoteControl`

## Open Product Risks

- The ASP.NET Core/Kestrel gRPC host is not packageable as the Android app-side transport because `Microsoft.AspNetCore.App` has no `android-arm64` runtime pack; Android uses the runtime/bridge path instead.
- Physical-device Android bridge acceptance has passed for the probe package; broader emulator/device matrix coverage remains future compatibility work.
- Property mutation can produce app side effects and needs strict policy controls.
- Public GitHub plus private Azure DevOps needs a clear release/source-of-truth policy before publishing.
