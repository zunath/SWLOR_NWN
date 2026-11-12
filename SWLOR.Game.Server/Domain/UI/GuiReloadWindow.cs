namespace SWLOR.Game.Server.Domain.UI
{
    /// <summary>
    /// A window a hot reload closed in the old build and reopens in the new one (<see cref="IGuiService.CloseWindowsForReload"/>):
    /// its type, the object it was shown on and the object it was tethered to.
    /// </summary>
    /// <param name="Type">The window.</param>
    /// <param name="ShownOn">The object the window was shown on: the player, or a creature a DM controls.</param>
    /// <param name="Tether">The object the window was tethered to, or <c>OBJECT_INVALID</c>.</param>
    public sealed record GuiReloadWindow(GuiWindowType Type, uint ShownOn, uint Tether);
}
