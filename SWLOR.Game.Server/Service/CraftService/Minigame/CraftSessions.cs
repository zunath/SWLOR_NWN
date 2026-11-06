using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Crafting;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The entry point crafting windows use to start, finish and settle a craft. It loads the player's record and runs
/// <see cref="CraftSessionLifecycle"/> against the game engine.
/// </summary>
public static class CraftSessions
{
    private static ICraftSessionsService Instance => FacadeAccessor.Resolve<ICraftSessionsService>();

    /// <summary>Commits the materials for a craft. Anything still saved from an earlier craft is settled as a failure first.</summary>
    public static CraftSessionStartResult Start(uint player, CraftSessionStartRequest request)
    {
        return Instance.Start(player, request);
    }

    /// <summary>
    /// Creates the crafted item. Returns null when the session already ended, so a repeated request does nothing. The engine's
    /// <paramref name="actionsTaken"/> (its <c>Turn</c>) and the time since the craft began hold the XP to the modeled pace.
    /// </summary>
    public static CraftSessionSuccess Succeed(uint player, CraftSession session, MinigameMeter quality, int actionsTaken)
    {
        return Instance.Succeed(player, session, quality, actionsTaken);
    }

    /// <summary>
    /// Ends the player's saved craft as a failure. Does nothing when no craft is saved. A window passes the engine's
    /// <paramref name="actionsTaken"/>; null (a settlement with no window) uses the count the craft saved. Either way the failure XP is
    /// held to the modeled pace.
    /// </summary>
    public static CraftSettlement Fail(uint player, CraftSessionEndReason reason, int? actionsTaken = null)
    {
        return Instance.Fail(player, reason, actionsTaken);
    }

    /// <summary>Saves the actions the player's craft has resolved, so a settlement with no window holds its XP to the same pace.</summary>
    public static void RecordActions(uint player, CraftSession session, int actionsTaken)
    {
        Instance.RecordActions(player, session, actionsTaken);
    }

    /// <summary>Settles a craft nothing is playing and returns held items that now fit. Call at login and when a crafting window opens.</summary>
    public static CraftSettlement SettleLeftover(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        return Instance.SettleLeftover(player, reason);
    }

    /// <summary>
    /// Settles what a crash left between the player record and the character file, then settles a craft left over as
    /// <see cref="SettleLeftover"/> does. Call it once when a player enters the module.
    /// </summary>
    public static CraftSettlement RecoverAtLogin(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        return Instance.RecoverAtLogin(player, reason);
    }

    /// <summary>Gives the XP a craft earned while the player was dead, once they are alive. Call at a respawn.</summary>
    public static bool GrantPendingXp(uint player)
    {
        return Instance.GrantPendingXp(player);
    }

    /// <summary>True while the player has a craft saved, whichever window is playing it.</summary>
    public static bool IsCrafting(uint player)
    {
        return Instance.IsCrafting(player);
    }

    /// <summary>True while the session is still the player's saved craft: it has not been settled by a death, logout or window close.</summary>
    public static bool IsActive(uint player, CraftSession session)
    {
        return Instance.IsActive(player, session);
    }
}
