using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <summary>
/// The entry point crafting windows use to start, finish and settle a craft. It loads the player's record and runs
/// <see cref="CraftSessionLifecycle"/> against the game engine. Static callers reach
/// the container's instance through <c>GameServices.CraftSessions</c>.
/// </summary>
public interface ICraftSessionsService
{
    /// <summary>Commits the materials for a craft. Anything still saved from an earlier craft is settled as a failure first.</summary>
    CraftSessionStartResult Start(uint player, CraftSessionStartRequest request);

    /// <summary>
    /// Creates the crafted item. Returns null when the session already ended, so a repeated request does nothing. The engine's
    /// <paramref name="actionsTaken"/> (its <c>Turn</c>) and the time since the craft began hold the XP to the modeled pace.
    /// </summary>
    CraftSessionSuccess Succeed(uint player, CraftSession session, MinigameMeter quality, int actionsTaken);

    /// <summary>
    /// Ends the player's saved craft as a failure. Does nothing when no craft is saved. A window passes the engine's
    /// <paramref name="actionsTaken"/>; null (a settlement with no window) uses the count the craft saved. Either way the failure XP is
    /// held to the modeled pace.
    /// </summary>
    CraftSettlement Fail(uint player, CraftSessionEndReason reason, int? actionsTaken = null);

    /// <summary>Saves the actions the player's craft has resolved, so a settlement with no window holds its XP to the same pace.</summary>
    void RecordActions(uint player, CraftSession session, int actionsTaken);

    /// <summary>Settles a craft nothing is playing and returns held items that now fit. Call at login and when a crafting window opens.</summary>
    CraftSettlement SettleLeftover(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted);

    /// <summary>
    /// Settles what a crash left between the player record and the character file, then settles a craft left over as
    /// <see cref="SettleLeftover"/> does. Call it once when a player enters the module.
    /// </summary>
    CraftSettlement RecoverAtLogin(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted);

    /// <summary>Gives the XP a craft earned while the player was dead, once they are alive. Call at a respawn.</summary>
    bool GrantPendingXp(uint player);

    /// <summary>True while the player has a craft saved, whichever window is playing it.</summary>
    bool IsCrafting(uint player);

    /// <summary>True while the session is still the player's saved craft: it has not been settled by a death, logout or window close.</summary>
    bool IsActive(uint player, CraftSession session);
}
