namespace SWLOR.Toolset.Settings
{
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
    }
}
