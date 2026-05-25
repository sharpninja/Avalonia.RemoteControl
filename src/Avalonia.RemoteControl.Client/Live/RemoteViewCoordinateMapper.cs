namespace Avalonia.RemoteControl.Client.Live;

/// <summary>
/// Maps live-view coordinates between the local viewport and remote root DIP space.
/// </summary>
public sealed class RemoteViewCoordinateMapper
{
    private RemoteViewCoordinateMapper(
        double remoteWidth,
        double remoteHeight,
        double viewportWidth,
        double viewportHeight,
        double scale,
        double offsetX,
        double offsetY)
    {
        RemoteWidth = remoteWidth;
        RemoteHeight = remoteHeight;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Scale = scale;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    /// <summary>
    /// Gets the remote root width in DIPs.
    /// </summary>
    public double RemoteWidth { get; }

    /// <summary>
    /// Gets the remote root height in DIPs.
    /// </summary>
    public double RemoteHeight { get; }

    /// <summary>
    /// Gets the local viewport width.
    /// </summary>
    public double ViewportWidth { get; }

    /// <summary>
    /// Gets the local viewport height.
    /// </summary>
    public double ViewportHeight { get; }

    /// <summary>
    /// Gets the fitted scale.
    /// </summary>
    public double Scale { get; }

    /// <summary>
    /// Gets the fitted content offset X.
    /// </summary>
    public double OffsetX { get; }

    /// <summary>
    /// Gets the fitted content offset Y.
    /// </summary>
    public double OffsetY { get; }

    /// <summary>
    /// Creates a mapper for a fitted live-view surface.
    /// </summary>
    /// <param name="remoteWidth">Remote root width in DIPs.</param>
    /// <param name="remoteHeight">Remote root height in DIPs.</param>
    /// <param name="viewportWidth">Local viewport width.</param>
    /// <param name="viewportHeight">Local viewport height.</param>
    /// <returns>The coordinate mapper.</returns>
    public static RemoteViewCoordinateMapper Create(
        double remoteWidth,
        double remoteHeight,
        double viewportWidth,
        double viewportHeight)
    {
        if (remoteWidth <= 0 || remoteHeight <= 0 || viewportWidth <= 0 || viewportHeight <= 0)
        {
            return new RemoteViewCoordinateMapper(remoteWidth, remoteHeight, viewportWidth, viewportHeight, 1, 0, 0);
        }

        var scale = Math.Min(viewportWidth / remoteWidth, viewportHeight / remoteHeight);
        var contentWidth = remoteWidth * scale;
        var contentHeight = remoteHeight * scale;

        return new RemoteViewCoordinateMapper(
            remoteWidth,
            remoteHeight,
            viewportWidth,
            viewportHeight,
            scale,
            (viewportWidth - contentWidth) / 2,
            (viewportHeight - contentHeight) / 2);
    }

    /// <summary>
    /// Converts a local viewport point to remote root DIP coordinates.
    /// </summary>
    /// <param name="x">Local X.</param>
    /// <param name="y">Local Y.</param>
    /// <returns>Remote point.</returns>
    public RemoteViewPoint ToRemote(double x, double y)
    {
        return new RemoteViewPoint(
            (x - OffsetX) / Scale,
            (y - OffsetY) / Scale);
    }

    /// <summary>
    /// Converts a remote root DIP point to local viewport coordinates.
    /// </summary>
    /// <param name="x">Remote X.</param>
    /// <param name="y">Remote Y.</param>
    /// <returns>Local point.</returns>
    public RemoteViewPoint ToViewport(double x, double y)
    {
        return new RemoteViewPoint(
            x * Scale + OffsetX,
            y * Scale + OffsetY);
    }
}

/// <summary>
/// Lightweight coordinate pair for live-view mapping.
/// </summary>
/// <param name="X">X coordinate.</param>
/// <param name="Y">Y coordinate.</param>
public sealed record RemoteViewPoint(double X, double Y);
