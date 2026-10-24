using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>One of the three sub-assemblies of a piece.</summary>
public enum SubAssembly
{
    Frame = 0,
    Shell = 1,
    Fittings = 2
}

/// <summary>The fabricator's three tool heads. Each serves one sub-assembly; one head is active at a time.</summary>
public enum FabricatorHead
{
    Cutter = 0,
    Extruder = 1,
    Bonder = 2
}

public static class FabricatorHeads
{
    public static readonly IReadOnlyList<SubAssembly> AllParts = new[] { SubAssembly.Frame, SubAssembly.Shell, SubAssembly.Fittings };

    public static readonly IReadOnlyList<FabricatorHead> AllHeads = new[] { FabricatorHead.Cutter, FabricatorHead.Extruder, FabricatorHead.Bonder };

    /// <summary>The sub-assembly the head works on: the Cutter the Frame, the Extruder the Shell and the Bonder the Fittings.</summary>
    public static SubAssembly Serves(this FabricatorHead head) => (SubAssembly)(int)head;

    /// <summary>The head that serves the sub-assembly.</summary>
    public static FabricatorHead HeadFor(this SubAssembly part) => (FabricatorHead)(int)part;

    /// <summary>The two other sub-assemblies, in the order Frame, Shell, Fittings.</summary>
    public static (SubAssembly First, SubAssembly Second) OtherParts(this SubAssembly part) => part switch
    {
        SubAssembly.Frame => (SubAssembly.Shell, SubAssembly.Fittings),
        SubAssembly.Shell => (SubAssembly.Frame, SubAssembly.Fittings),
        _ => (SubAssembly.Frame, SubAssembly.Shell)
    };

    public static string RunActionId(this SubAssembly part) => part switch
    {
        SubAssembly.Frame => FabricatorActionIds.RunFrame,
        SubAssembly.Shell => FabricatorActionIds.RunShell,
        _ => FabricatorActionIds.RunFittings
    };

    public static string RetoolActionId(this FabricatorHead head) => head switch
    {
        FabricatorHead.Cutter => FabricatorActionIds.RetoolCutter,
        FabricatorHead.Extruder => FabricatorActionIds.RetoolExtruder,
        _ => FabricatorActionIds.RetoolBonder
    };
}
