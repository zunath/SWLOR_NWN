namespace SWLOR.Game.Server.Service.CraftService.Minigame.Forge;

/// <summary>Everything the Plasma Forge knows about an attempt: the shared meters, the telegraphed opportunity, Thermal, whether the piece is set, and what the crafter's perks count.</summary>
/// <param name="Meters">Shape is the progress meter, Finish the quality meter, and Integrity the piece's soundness.</param>
/// <param name="NextOpportunity">Stable Containment when it is announced for the next action, otherwise null.</param>
/// <param name="Thermal">The piece's heat, 0 to 100.</param>
/// <param name="Quenched">True once Cryo-Quench has been used. The piece is set: Ramp Plasma and Hydro-Press are disabled.</param>
/// <param name="PressStreak">Hydro-Presses made in a row just now, up to Hammer Rhythm's cap; any other action resets it. Counted only with Hammer Rhythm.</param>
/// <param name="TemperHold">Temper Hold's countdown: 0 when the piece cools as usual, and above 0 while it is holding.</param>
/// <param name="RedLineUsed">How many Red Line Presses this craft has made.</param>
/// <param name="LastWasOverdrive">True when the last action was an Overdrive Press, which cannot be used twice in a row.</param>
public sealed record ForgeState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    int Thermal,
    bool Quenched,
    int PressStreak = 0,
    int TemperHold = 0,
    int RedLineUsed = 0,
    bool LastWasOverdrive = false) : IMinigameState<ForgeState>
{
    public ForgeState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    /// <summary>True when Stable Containment is announced for the next action.</summary>
    public bool HasStableContainment => NextOpportunity?.Id == ForgeRules.StableContainmentId;
}
