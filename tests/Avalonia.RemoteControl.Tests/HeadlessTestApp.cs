using Avalonia;
using Avalonia.Headless;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.RemoteControl.Tests;
using Avalonia.RemoteControl.Tool.Docking;
using Dock.Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(HeadlessTestApp))]

namespace Avalonia.RemoteControl.Tests;

/// <summary>
/// Shared headless Avalonia application for xunit.v3 [AvaloniaFact]/[AvaloniaTheory] tests.
/// Includes the Fluent and Dock themes so DockControl and docked chrome render headlessly.
/// </summary>
public sealed class HeadlessTestApp : Application
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadlessTestApp"/> class.
    /// </summary>
    public HeadlessTestApp()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new DockFluentTheme());
        DataTemplates.Add(new RemoteControlDockViewLocator());
    }

    /// <summary>
    /// Builds the headless Avalonia application for the test session.
    /// </summary>
    /// <returns>The configured app builder.</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<HeadlessTestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
