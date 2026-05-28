# Manual Acceptance Evidence

## 2026-05-25 Evidence

This file records the current acceptance evidence for manual requirements. Automated integration tests are used where they exercise the same user-visible workflow without requiring interactive desktop operation.

Commands run:

```powershell
dotnet restore Avalonia.RemoteControl.slnx
dotnet build Avalonia.RemoteControl.slnx --configuration Release --no-restore
dotnet test Avalonia.RemoteControl.slnx --configuration Release --no-build --logger "console;verbosity=normal"
dotnet pack Avalonia.RemoteControl.slnx --configuration Release --no-build --output artifacts/packages
dotnet build .\samples\Avalonia.RemoteControl.AndroidProbe.Android\Avalonia.RemoteControl.AndroidProbe.Android.csproj -c Debug -f net10.0-android
dotnet run --project src\Avalonia.RemoteControl.Tool\Avalonia.RemoteControl.Tool.csproj --configuration Release --no-build -- --help
```

Results:

- Solution build passed with zero warnings and zero errors.
- Android probe build passed with zero warnings and zero errors.
- Test suite passed with 73 passed, zero failed, and zero skipped.
- Package generation produced Runtime, Server, and Tool NuGet and symbol packages.
- Tool help displayed Local, Network, and ADB connection modes.

Requirement evidence:

- `TEST-MANUAL-001`: `RemoteControlDesktopSessionTests.DesktopSessionReadsCapabilitiesFromHostedServer` proves loopback client connection to a hosted debuggee endpoint.
- `TEST-MANUAL-002`: `RemoteControlDesktopSessionTests.DesktopSessionTrustsConfiguredTlsCertificate` and `DesktopSessionTrustsAcceptedTlsCertificateFingerprint` prove TLS/token connection with configured and accepted certificate trust.
- `TEST-MANUAL-003`: Physical device `ZD222QH58Q` installed/launched the Android probe, read the package-private marker through `run-as`, connected through `avalonia-remote adb connect --keep-forward`, captured a 31-node snapshot, removed the ADB forward, and stopped the probe.
- `TEST-MANUAL-004`: `RemoteControlTreeStreamTests.TreeStreamEmitsSnapshotsUntilCanceled` proves live tree update streaming.
- `TEST-MANUAL-005`: `RemoteControlDesktopSessionTests.DesktopSessionStreamsHostedLogs` proves log streaming through the desktop client session.
- `TEST-MANUAL-006`: `RemoteControlCommandTests.GrpcInvokeClickUsesActionPolicy` and click invocation tests prove remote click dispatch.
- `TEST-MANUAL-007`: `RemoteControlCommandTests.GrpcSetPropertyUsesMutationPolicy` and property mutation tests prove allowed property edit behavior.
- `TEST-MANUAL-008`: `RemoteControlCommandTests.PropertyMutationAuditLogIncludesClientIdentity` and `ClickInvocationAuditLogIncludesClientIdentity` prove mutation/action audit trail identity.

Residual risk:

- Interactive desktop client visual QA remains useful before a release milestone, but the current functional acceptance evidence covers the required connection, inspection, log, action, mutation, audit, TLS, and Android ADB workflows.
