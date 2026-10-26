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

    /// <summary>Creates the crafted item. Returns null when the session already ended, so a repeated request does nothing.</summary>
    public static CraftSessionSuccess Succeed(uint player, CraftSession session, MinigameMeter quality)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? null : Lifecycle.Succeed(player, dbPlayer, session, quality);
    }

    /// <summary>Ends the player's saved craft as a failure. Does nothing when no craft is saved.</summary>
    public static CraftSettlement Fail(uint player, CraftSessionEndReason reason)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.Fail(player, dbPlayer, reason);
    }

    /// <summary>Settles a craft nothing is playing and returns held items that now fit. Call at login and when a crafting window opens.</summary>
    public static CraftSettlement SettleLeftover(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.SettleLeftover(player, dbPlayer, reason);
    }

    /// <summary>Gives the XP a craft earned while the player was dead, once they are alive. Call at a respawn.</summary>
    public static bool GrantPendingXp(uint player)
    {
        var dbPlayer = DB.Get<Player>(GetObjectUUID(player));
        return dbPlayer != null && Lifecycle.GrantPendingXp(player, dbPlayer);
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
