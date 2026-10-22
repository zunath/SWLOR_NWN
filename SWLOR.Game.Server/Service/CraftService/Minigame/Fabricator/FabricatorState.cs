namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>How far one sub-assembly has come: its progress toward its size, and its quality from 0 to 100.</summary>
public readonly record struct SubAssemblyState(int Progress, int Quality);

/// <summary>
/// Everything the Fabricator knows about an attempt. The shared meters sum or summarize the three sub-assemblies: the progress
/// meter is the sum of their progress and the quality meter is their average capped at the weakest + 20, and the engine keeps both
/// in step with the sub-assemblies after every action.
/// </summary>
/// <param name="Meters">Progress is the sum of the sub-assemblies, Quality their capped average, Integrity the Machine Condition.</param>
/// <param name="NextOpportunity">Prime Feedstock while one is announced or waiting for its head, otherwise null.</param>
/// <param name="ActiveHead">The tool head that is loaded.</param>
/// <param name="Frame">The Frame, worked by the Cutter.</param>
/// <param name="Shell">The Shell, worked by the Extruder.</param>
/// <param name="Fittings">The Fittings, worked by the Bonder.</param>
/// <param name="PrimedHead">The head Prime Feedstock names, or null when none is.</param>
/// <param name="PrimeActionsLeft">How many more actions the prime stays in reach, counting the next one.</param>
/// <param name="Assembled">True once Assemble has finished the item.</param>
public sealed record FabricatorState(
    MinigameMeters Meters,
    MinigameOpportunity NextOpportunity,
    FabricatorHead ActiveHead,
    SubAssemblyState Frame,
    SubAssemblyState Shell,
    SubAssemblyState Fittings,
    FabricatorHead? PrimedHead,
    int PrimeActionsLeft,
    bool Assembled) : IMinigameState<FabricatorState>
{
    public FabricatorState WithMeters(MinigameMeters meters) => this with { Meters = meters };

    public SubAssemblyState Part(SubAssembly part) => part switch
    {
        SubAssembly.Frame => Frame,
        SubAssembly.Shell => Shell,
        _ => Fittings
    };

    public FabricatorState WithPart(SubAssembly part, SubAssemblyState value) => part switch
    {
        SubAssembly.Frame => this with { Frame = value },
        SubAssembly.Shell => this with { Shell = value },
        _ => this with { Fittings = value }
    };

    /// <summary>True when Prime Feedstock waits for the given head: its next action gets the bonus.</summary>
    public bool IsPrimed(FabricatorHead head) => PrimedHead == head;

    /// <summary>The state with the prime used up or passed.</summary>
    public FabricatorState WithoutPrime() => this with { NextOpportunity = null, PrimedHead = null, PrimeActionsLeft = 0 };
}
