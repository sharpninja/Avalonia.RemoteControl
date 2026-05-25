using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.RemoteControl.AndroidProbe.Views;

namespace Avalonia.RemoteControl.AndroidProbe;

/// <summary>
/// Defines the Android probe Avalonia application.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the root provider used by the Android bridge host.
    /// </summary>
    public static ProbeRootProvider RootProvider { get; } = new();

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            var view = new ProbeView();
            RootProvider.SetRoot(view);
            singleView.MainView = view;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
