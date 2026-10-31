using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Finishes a craft the way a player does at the pace the XP multipliers assume: the fake clock moves by the actions' modeled
/// seconds before the craft succeeds, so the XP is the full amount unless a test chooses otherwise.
/// </summary>
internal static class CraftSessionTestPace
{
    /// <summary>A craft's worth of actions for the tests that only need some.</summary>
    public const int Actions = 15;

    public static CraftSessionSuccess SucceedAtPace(
        this CraftSessionLifecycle lifecycle,
        FakeCraftSessionWorld world,
        uint player,
        Player dbPlayer,
        CraftSession session,
        MinigameMeter quality,
        int actions = Actions)
    {
        world.AdvanceByActions(actions);
        return lifecycle.Succeed(player, dbPlayer, session, quality, actions);
    }

    /// <summary>Breaks a craft the way a window does after playing its actions at the modeled pace, so the failure XP is the full amount.</summary>
    public static CraftSettlement FailAtPace(
        this CraftSessionLifecycle lifecycle,
        FakeCraftSessionWorld world,
        uint player,
        Player dbPlayer,
        CraftSessionEndReason reason,
        int actions = Actions)
    {
        world.AdvanceByActions(actions);
        return lifecycle.Fail(player, dbPlayer, reason, actions);
    }
}
