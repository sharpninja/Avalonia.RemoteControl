using Avalonia;
using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Simple four-region dock layout for the main shell workspace.
/// </summary>
public sealed class DockLayout : Panel
{
    /// <summary>
    /// Defines the dock region attached property.
    /// </summary>
    public static readonly AttachedProperty<DockRegion> RegionProperty =
        AvaloniaProperty.RegisterAttached<DockLayout, Control, DockRegion>(
            "Region",
            DockRegion.Fill,
            inherits: false);

    /// <summary>
    /// Defines the west region width property.
    /// </summary>
    public static readonly StyledProperty<double> WestWidthProperty =
        AvaloniaProperty.Register<DockLayout, double>(nameof(WestWidth), 340);

    /// <summary>
    /// Defines the east region width property.
    /// </summary>
    public static readonly StyledProperty<double> EastWidthProperty =
        AvaloniaProperty.Register<DockLayout, double>(nameof(EastWidth), 390);

    /// <summary>
    /// Defines the south region height property.
    /// </summary>
    public static readonly StyledProperty<double> SouthHeightProperty =
        AvaloniaProperty.Register<DockLayout, double>(nameof(SouthHeight), 220);

    /// <summary>
    /// Defines the separator spacing property.
    /// </summary>
    public static readonly StyledProperty<double> DockSpacingProperty =
        AvaloniaProperty.Register<DockLayout, double>(nameof(DockSpacing), 5);

    static DockLayout()
    {
        AffectsMeasure<DockLayout>(
            WestWidthProperty,
            EastWidthProperty,
            SouthHeightProperty,
            DockSpacingProperty);
        RegionProperty.Changed.AddClassHandler<Control>((control, _) =>
        {
            if (control.Parent is DockLayout layout)
            {
                layout.InvalidateMeasure();
            }
        });
    }

    /// <summary>
    /// Gets or sets the west region width.
    /// </summary>
    public double WestWidth
    {
        get => GetValue(WestWidthProperty);
        set => SetValue(WestWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the east region width.
    /// </summary>
    public double EastWidth
    {
        get => GetValue(EastWidthProperty);
        set => SetValue(EastWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets the south region height.
    /// </summary>
    public double SouthHeight
    {
        get => GetValue(SouthHeightProperty);
        set => SetValue(SouthHeightProperty, value);
    }

    /// <summary>
    /// Gets or sets the spacing between dock regions.
    /// </summary>
    public double DockSpacing
    {
        get => GetValue(DockSpacingProperty);
        set => SetValue(DockSpacingProperty, value);
    }

    /// <summary>
    /// Gets the dock region for a child control.
    /// </summary>
    /// <param name="element">Child control.</param>
    /// <returns>The dock region.</returns>
    public static DockRegion GetRegion(Control element)
    {
        return element.GetValue(RegionProperty);
    }

    /// <summary>
    /// Sets the dock region for a child control.
    /// </summary>
    /// <param name="element">Child control.</param>
    /// <param name="value">Dock region.</param>
    public static void SetRegion(Control element, DockRegion value)
    {
        element.SetValue(RegionProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var sizes = CalculateRegionSizes(availableSize);
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            child.Measure(GetConstraintForRegion(GetRegion(child), sizes));
        }

        return availableSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var sizes = CalculateRegionSizes(finalSize);
        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            child.Arrange(GetBoundsForRegion(GetRegion(child), finalSize, sizes));
        }

        return finalSize;
    }

    private RegionSizes CalculateRegionSizes(Size availableSize)
    {
        var visibleRegions = GetVisibleRegions();
        var spacing = Math.Max(0, DockSpacing);
        var width = Math.Max(0, availableSize.Width);
        var height = Math.Max(0, availableSize.Height);
        var west = visibleRegions.West ? Clamp(WestWidth, 0, Math.Max(0, width - spacing)) : 0;
        var east = visibleRegions.East ? Clamp(EastWidth, 0, Math.Max(0, width - west - spacing)) : 0;
        var contentWidth = Math.Max(
            0,
            width - west - east - (HasSideRegion(west) ? spacing : 0) - (HasSideRegion(east) ? spacing : 0));
        var south = visibleRegions.South ? Clamp(SouthHeight, 0, Math.Max(0, height - spacing)) : 0;
        var contentHeight = Math.Max(0, height - south - (south > 0 ? spacing : 0));

        return new RegionSizes(west, east, south, contentWidth, contentHeight, spacing);
    }

    private VisibleRegions GetVisibleRegions()
    {
        var west = false;
        var east = false;
        var south = false;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }

            switch (GetRegion(child))
            {
                case DockRegion.West:
                    west = true;
                    break;
                case DockRegion.East:
                    east = true;
                    break;
                case DockRegion.South:
                    south = true;
                    break;
            }
        }

        return new VisibleRegions(west, east, south);
    }

    private static Size GetConstraintForRegion(DockRegion region, RegionSizes sizes)
    {
        return region switch
        {
            DockRegion.West => new Size(sizes.WestWidth, double.PositiveInfinity),
            DockRegion.East => new Size(sizes.EastWidth, double.PositiveInfinity),
            DockRegion.South => new Size(sizes.ContentWidth, sizes.SouthHeight),
            _ => new Size(sizes.ContentWidth, sizes.ContentHeight),
        };
    }

    private static Rect GetBoundsForRegion(DockRegion region, Size finalSize, RegionSizes sizes)
    {
        var westSpacing = HasSideRegion(sizes.WestWidth) ? sizes.Spacing : 0;
        var eastSpacing = HasSideRegion(sizes.EastWidth) ? sizes.Spacing : 0;
        var contentX = sizes.WestWidth + westSpacing;
        var southY = finalSize.Height - sizes.SouthHeight;

        return region switch
        {
            DockRegion.West => new Rect(0, 0, sizes.WestWidth, finalSize.Height),
            DockRegion.East => new Rect(finalSize.Width - sizes.EastWidth, 0, sizes.EastWidth, finalSize.Height),
            DockRegion.South => new Rect(contentX, southY, sizes.ContentWidth, sizes.SouthHeight),
            _ => new Rect(contentX, 0, sizes.ContentWidth, sizes.ContentHeight),
        };
    }

    private static bool HasSideRegion(double size)
    {
        return size > 0;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        return Math.Min(Math.Max(value, minimum), maximum);
    }

    private readonly record struct RegionSizes(
        double WestWidth,
        double EastWidth,
        double SouthHeight,
        double ContentWidth,
        double ContentHeight,
        double Spacing);

    private readonly record struct VisibleRegions(bool West, bool East, bool South);
}

/// <summary>
/// Main-shell dock regions.
/// </summary>
public enum DockRegion
{
    /// <summary>
    /// Fills the undeclared workspace region.
    /// </summary>
    Fill,

    /// <summary>
    /// Docks to the west edge.
    /// </summary>
    West,

    /// <summary>
    /// Docks to the east edge.
    /// </summary>
    East,

    /// <summary>
    /// Docks to the south edge.
    /// </summary>
    South,
}
