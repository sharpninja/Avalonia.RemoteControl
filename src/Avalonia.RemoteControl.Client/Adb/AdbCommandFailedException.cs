namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Indicates that an adb command failed.
/// </summary>
public sealed class AdbCommandFailedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdbCommandFailedException"/> class.
    /// </summary>
    /// <param name="message">The sanitized failure message.</param>
    /// <param name="result">The adb command result.</param>
    public AdbCommandFailedException(string message, AdbCommandResult result)
        : base(message)
    {
        Result = result;
    }

    /// <summary>
    /// Gets the adb command result.
    /// </summary>
    public AdbCommandResult Result { get; }
}
