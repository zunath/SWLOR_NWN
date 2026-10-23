namespace SWLOR.Game.Server.Service.CraftService.Minigame.Fabricator;

/// <summary>The ids of the Fabricator actions and of its telegraphed opportunity.</summary>
public static class FabricatorActionIds
{
    public const string RunFrame = "run_frame";
    public const string RunShell = "run_shell";
    public const string RunFittings = "run_fittings";
    public const string PrecisionPass = "precision_pass";
    public const string RetoolCutter = "retool_cutter";
    public const string RetoolExtruder = "retool_extruder";
    public const string RetoolBonder = "retool_bonder";
    public const string Service = "service";
    public const string Assemble = "assemble";

    /// <summary>
    /// Dual Feed runs the loaded head's sub-assembly and one other at once. The first and second are the other two sub-assemblies in
    /// the order Frame, Shell, Fittings, so with the Extruder loaded the first is the Frame and the second the Fittings.
    /// </summary>
    public const string DualFeedFirst = "dual_feed_first";

    public const string DualFeedSecond = "dual_feed_second";

    public const string PrimeFeedstock = "prime_feedstock";
}
