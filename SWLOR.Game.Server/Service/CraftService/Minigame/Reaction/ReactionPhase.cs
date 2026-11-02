namespace SWLOR.Game.Server.Service.CraftService.Minigame.Reaction;

/// <summary>A phase of the reaction cycle. Every cycle runs Calm, Rising, Peak, Settling and then starts over.</summary>
public enum ReactionPhase
{
    /// <summary>The mixture is quiet. Charge is safe.</summary>
    Calm = 1,

    /// <summary>The mixture is building toward its Peak.</summary>
    Rising = 2,

    /// <summary>The mixture is at its most violent. Charge is doubled and Refine is halved.</summary>
    Peak = 3,

    /// <summary>The mixture is cooling. Refine is doubled.</summary>
    Settling = 4
}
