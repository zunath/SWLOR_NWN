namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// A telegraphed chance the player can plan around, such as Stable Containment. It is always announced at least one
/// turn before it arrives and only ever helps.
/// </summary>
public sealed record MinigameOpportunity
{
    public MinigameOpportunity(string id, string name, string description, int turnsAway)
    {
        if (turnsAway < 1)
            throw new ArgumentOutOfRangeException(nameof(turnsAway), "An opportunity is telegraphed at least one turn ahead.");

        Id = id;
        Name = name;
        Description = description;
        TurnsAway = turnsAway;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }

    /// <summary>The number of actions before it applies; 1 means the next action.</summary>
    public int TurnsAway { get; }
}
