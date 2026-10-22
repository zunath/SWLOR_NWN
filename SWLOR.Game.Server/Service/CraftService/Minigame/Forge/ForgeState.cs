namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>Everything the Plasma Forge knows about an attempt: the shared meters, the telegraphed opportunity, Thermal and whether the piece is set.</summary>
/// <param name="Meters">Shape is the progress meter, Finish the quality meter, and Integrity the piece's soundness.</param>
/// <param name="NextOpportunity">Stable Containment when it is announced for the next action, otherwise null.</param>
/// <param name="Thermal">The piece's heat, 0 to 100.</param>
/// <param name="Quenched">True once Cryo-Quench has been used. The piece is set: Ramp Plasma and Hydro-Press are disabled.</param>
public sealed record ForgeState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    int Thermal,
    bool Quenched) : IMinigameState<ForgeState>
{
    public ForgeState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>True when Stable Containment is announced for the next action.</summary>
    public bool HasStableContainment => NextOpportunity?.Id == ForgeRules.StableContainmentId;
}
