using SWLOR.Game.Server.Entity;

namespace SWLOR.Game.Server.Service.CraftService.Minigame;

/// <summary>
/// The entry point crafting windows use to start, finish and settle a craft. It loads the player's record and runs
/// <see cref="CraftSessionLifecycle"/> against the game engine.
/// </summary>
public static class CraftSessionService
{
    private static readonly CraftSessionLifecycle Lifecycle =
        new(NwnCraftSessionWorld.Instance, ServiceMinigameRandom.Instance);

    /// <summary>Commits the materials for a craft. Anything still saved from an earlier craft is settled as a failure first.</summary>
    public static CraftSessionStartResult Start(uint player, CraftSessionStartRequest request)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null
            ? CraftSessionStartResult.Refused("Your character could not be loaded.")
            : Lifecycle.Start(player, dbPlayer, request);
    }

    /// <summary>
    /// Creates the crafted item. Returns null when the session already ended, so a repeated request does nothing. The engine's
    /// <paramref name="actionsTaken"/> (its <c>Turn</c>) and the time since the craft began hold the XP to the modeled pace.
    /// </summary>
    public static CraftSessionSuccess Succeed(uint player, CraftSession session, MinigameMeter quality, int actionsTaken)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? null : Lifecycle.Succeed(player, dbPlayer, session, quality, actionsTaken);
    }

    /// <summary>
    /// Ends the player's saved craft as a failure. Does nothing when no craft is saved. A window passes the engine's
    /// <paramref name="actionsTaken"/>; null (a settlement with no window) uses the count the craft saved. Either way the failure XP is
    /// held to the modeled pace.
    /// </summary>
    public static CraftSettlement Fail(uint player, CraftSessionEndReason reason, int? actionsTaken = null)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.Fail(player, dbPlayer, reason, actionsTaken);
    }

    /// <summary>Saves the actions the player's craft has resolved, so a settlement with no window holds its XP to the same pace.</summary>
    public static void RecordActions(uint player, CraftSession session, int actionsTaken)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        if (dbPlayer != null)
            Lifecycle.RecordActions(dbPlayer, session, actionsTaken);
    }

    /// <summary>Settles a craft nothing is playing and returns held items that now fit. Call at login and when a crafting window opens.</summary>
    public static CraftSettlement SettleLeftover(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.SettleLeftover(player, dbPlayer, reason);
    }

    /// <summary>
    /// Settles what a crash left between the player record and the character file, then settles a craft left over as
    /// <see cref="SettleLeftover"/> does. Call it once when a player enters the module.
    /// </summary>
    public static CraftSettlement RecoverAtLogin(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.RecoverAtLogin(player, dbPlayer, reason);
    }

    /// <summary>Gives the XP a craft earned while the player was dead, once they are alive. Call at a respawn.</summary>
    public static bool GrantPendingXp(uint player)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer != null && Lifecycle.GrantPendingXp(player, dbPlayer);
    }

    /// <summary>Lists a station fee a quick craft took, for the station's treasury once a character file holds it.</summary>
    public static void QueueStationFee(uint player, StationFeeCharge charge)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        if (dbPlayer != null)
            Lifecycle.QueueStationFee(player, dbPlayer, charge);
    }

    /// <summary>Credits the station treasuries with the player's fees a character file holds, each exactly once.</summary>
    public static bool CreditStationFees(uint player)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer != null && Lifecycle.CreditStationFees(player, dbPlayer);
    }

    /// <summary>True while the player has a craft saved, whichever window is playing it.</summary>
    public static bool IsCrafting(uint player)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer?.ActiveCraftSession != null;
    }

    /// <summary>True while the session is still the player's saved craft: it has not been settled by a death, logout or window close.</summary>
    public static bool IsActive(uint player, CraftSession session)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer != null && CraftSessionLifecycle.IsActive(dbPlayer, session);
    }
}
