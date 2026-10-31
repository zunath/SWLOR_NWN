namespace SWLOR.Game.Server.Service.AnimationService;

/// <summary>The gathering actions that have an authored clip. The Prospecting and Search engines map their actions onto these.</summary>
public enum GatheringAction
{
    /// <summary>Prospecting: Survey reveals a seam.</summary>
    Survey = 1,

    /// <summary>Prospecting: Strike.</summary>
    Strike = 2,

    /// <summary>Prospecting: Heavy Strike.</summary>
    HeavyStrike = 3,

    /// <summary>Prospecting: Extract claims a revealed seam.</summary>
    Extract = 4,

    /// <summary>Search: Search a spot of the board.</summary>
    Search = 5,

    /// <summary>Search: Dig Deeper into the same spot.</summary>
    Dig = 6
}
