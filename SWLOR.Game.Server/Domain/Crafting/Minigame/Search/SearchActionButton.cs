namespace SWLOR.Game.Server.Domain.Crafting.Minigame.Search;

/// <summary>
/// How an action button of the Search window is drawn. While the board is in play every action button stays clickable, even when its
/// action cannot be used: the client drops a click on a disabled button, and the first click on Search after choosing a spot often arrives
/// before the server's answer that enables it, so it was lost. The server checks every action and answers an unusable one with the reason,
/// so such a button only looks greyed.
/// </summary>
/// <param name="Enabled">Whether the client sends the button's clicks.</param>
/// <param name="LooksAvailable">Whether the button's text is drawn as usable rather than greyed.</param>
public sealed record SearchActionButton(bool Enabled, bool LooksAvailable)
{
    /// <summary>The button of an action.</summary>
    /// <param name="boardInPlay">True while the visit is in progress; false once it has ended.</param>
    /// <param name="view">The action as described for the chosen spot, or null when no spot is chosen.</param>
    public static SearchActionButton For(bool boardInPlay, SearchActionView view) =>
        new(boardInPlay, boardInPlay && view != null && view.Kind != SearchActionKind.Unavailable);
}
