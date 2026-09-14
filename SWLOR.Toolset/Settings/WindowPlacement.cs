namespace SWLOR.Toolset.Settings
{
    /// <summary>
    /// A connected display's bounds, in the same screen coordinates <see cref="WindowPlacement.Left"/>
    /// and <see cref="WindowPlacement.Top"/> are recorded in.
    /// </summary>
    public readonly record struct ScreenBounds(double Left, double Top, double Width, double Height);

    /// <summary>
    /// Where and how big a window was left: size, position, and whether it was maximised.
    /// </summary>
    /// <param name="Left">Screen X, or NaN when no position has been recorded.</param>
    /// <param name="Top">Screen Y, or NaN when no position has been recorded.</param>
    /// <remarks>
    /// Position is stored as well as size because restoring only the size moves a window a builder
    /// deliberately parked on a second monitor back onto the primary one.
    /// <para>
    /// A maximised window records the size it had before being maximised, not the screen's, so
    /// un-maximising after a restart gives back the window that was actually being used.
    /// </para>
    /// </remarks>
    public readonly record struct WindowPlacement(
        double Width, double Height, double Left, double Top, bool IsMaximized)
    {
        /// <summary>No window state recorded yet - a first run, so the window keeps its designed size.</summary>
        public static WindowPlacement Unset { get; } = new(0, 0, double.NaN, double.NaN, false);

        /// <summary>
        /// Smallest window worth restoring. A saved size below this is treated as absent rather than
        /// applied: a window can legitimately be reported at a few pixels while it is being torn down,
        /// and restoring that leaves a builder with a window they cannot find.
        /// </summary>
        public const double MinimumRestorableSize = 320;

        /// <summary>True when there is a size here worth applying to a window.</summary>
        public bool HasSize => Width >= MinimumRestorableSize && Height >= MinimumRestorableSize;

        /// <summary>True when there is a position here worth applying.</summary>
        public bool HasPosition => !double.IsNaN(Left) && !double.IsNaN(Top);

        /// <summary>
        /// How much of the title bar has to remain reachable for a saved position to be worth restoring,
        /// in screen pixels. Also the overhang allowed past a screen's left/top edge, which a window
        /// parked hard against the edge of a monitor legitimately has.
        /// </summary>
        public const double OnScreenMargin = 120;

        /// <summary>
        /// True when this position would leave a grabbable piece of title bar on one of
        /// <paramref name="screens"/>.
        /// </summary>
        /// <remarks>
        /// A saved position outlives the monitor it was saved on: undock a laptop, restart, and the
        /// remembered coordinates point at a display that no longer exists, which restores the window
        /// somewhere the builder cannot see or drag it back from. Only the top-left corner is checked,
        /// because that is what has to be reachable - and because size is recorded in device-independent
        /// units while position is in screen pixels, so the two cannot be intersected as one rectangle.
        /// <para>
        /// An empty screen list means the screens could not be enumerated, which is not evidence that the
        /// position is bad, so the saved placement is trusted.
        /// </para>
        /// </remarks>
        public bool IsOnAnyScreen(IReadOnlyList<ScreenBounds> screens)
        {
            if (!HasPosition)
                return false;

            if (screens == null || screens.Count == 0)
                return true;

            foreach (var screen in screens)
            {
                if (Left >= screen.Left - OnScreenMargin &&
                    Left <= screen.Left + screen.Width - OnScreenMargin &&
                    Top >= screen.Top - OnScreenMargin &&
                    Top <= screen.Top + screen.Height - OnScreenMargin)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
