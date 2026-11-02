namespace SWLOR.Game.Server.Service.CraftService.Minigame.Search;

/// <summary>
/// The ids of the Search board's three actions. An action is aimed at one spot, so an action id is the action and the spot together,
/// such as <c>search:4</c> for the centre spot. Spots count 0 to 8 from the top left, a row at a time.
/// </summary>
public static class SearchActionIds
{
    public const string Search = "search";
    public const string DigDeeper = "dig";
    public const string Sift = "sift";

    /// <summary>The id of the action aimed at a spot.</summary>
    public static string For(string action, int spot) => $"{action}:{spot}";

    /// <summary>Splits an id into its action and spot. False when the id is not one the board offers.</summary>
    public static bool TryParse(string actionId, out string action, out int spot)
    {
        action = null;
        spot = -1;
        if (string.IsNullOrEmpty(actionId))
            return false;

        var colon = actionId.IndexOf(':');
        if (colon <= 0 || colon == actionId.Length - 1)
            return false;

        var name = actionId[..colon];
        if (name != Search && name != DigDeeper && name != Sift)
            return false;

        if (!int.TryParse(actionId[(colon + 1)..], out var parsed) || parsed < 0 || parsed >= SearchBoard.Spots)
            return false;

        action = name;
        spot = parsed;
        return true;
    }
}
