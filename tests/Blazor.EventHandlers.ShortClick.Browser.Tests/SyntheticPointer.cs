namespace Blazor.EventHandlers.ShortClick.Browser.Tests;

/// <summary>
/// A pointer whose events a test dispatches by hand, to stage what real input cannot: several fingers at once, or a
/// pointerup after a pointercancel.
/// </summary>
/// <param name="PointerId">The id the browser would give it. A mouse is always 1, every finger gets a new one.</param>
/// <param name="PointerType">touch, mouse or pen.</param>
/// <param name="IsPrimary">True for a mouse or pen, and for the first finger of a touch gesture.</param>
/// <param name="OffsetPixels">How far right of the button's centre it presses.</param>
internal sealed record SyntheticPointer(int PointerId, string PointerType, bool IsPrimary, int OffsetPixels = 0)
{
    public static readonly SyntheticPointer Mouse = new(1, "mouse", IsPrimary: true, OffsetPixels: -60);

    public static readonly SyntheticPointer FirstFinger = new(2, "touch", IsPrimary: true);

    /// <summary>On the button, but far beyond the movement a short click allows from the first finger.</summary>
    public static readonly SyntheticPointer SecondFinger = new(3, "touch", IsPrimary: false, OffsetPixels: 80);
}
