using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Avalonia.RemoteControl.AndroidProbe.Android;

/// <summary>
/// Android application bootstrapper for the Avalonia remote-control probe app.
/// </summary>
[Application]
public sealed class Application : AvaloniaAndroidApplication<App>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Application"/> class.
    /// </summary>
    /// <param name="javaReference">Java object reference supplied by Android.</param>
    /// <param name="transfer">Ownership transfer mode for the Java reference.</param>
    public Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    /// <inheritdoc />
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
