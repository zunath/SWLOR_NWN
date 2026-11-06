using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService.Minigame;

namespace SWLOR.Game.Server.Domain.Crafting;

/// <inheritdoc cref="ICraftSessionsService" />
public class CraftSessionsService : ICraftSessionsService
{
    private readonly IDBService _db;
    private readonly ICraftService _craft;

    public CraftSessionsService(IDBService db, ICraftService craft)
    {
        _db = db;
        _craft = craft;
    }

    /// <summary>The rules of a craft, run against the engine world of the crafting sessions.</summary>
    protected virtual CraftSessionLifecycle Lifecycle =>
        new(_craft.SessionWorld, ServiceMinigameRandom.Instance);

    /// <inheritdoc />
    public CraftSessionStartResult Start(uint player, CraftSessionStartRequest request)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null
            ? CraftSessionStartResult.Refused("Your character could not be loaded.")
            : Lifecycle.Start(player, dbPlayer, request);
    }

    /// <inheritdoc />
    public CraftSessionSuccess Succeed(uint player, CraftSession session, MinigameMeter quality, int actionsTaken)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? null : Lifecycle.Succeed(player, dbPlayer, session, quality, actionsTaken);
    }

    /// <inheritdoc />
    public CraftSettlement Fail(uint player, CraftSessionEndReason reason, int? actionsTaken = null)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.Fail(player, dbPlayer, reason, actionsTaken);
    }

    /// <inheritdoc />
    public void RecordActions(uint player, CraftSession session, int actionsTaken)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        if (dbPlayer != null)
            Lifecycle.RecordActions(dbPlayer, session, actionsTaken);
    }

    /// <inheritdoc />
    public CraftSettlement SettleLeftover(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.SettleLeftover(player, dbPlayer, reason);
    }

    /// <inheritdoc />
    public CraftSettlement RecoverAtLogin(uint player, CraftSessionEndReason reason = CraftSessionEndReason.Interrupted)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer == null ? CraftSettlement.None : Lifecycle.RecoverAtLogin(player, dbPlayer, reason);
    }

    /// <inheritdoc />
    public bool GrantPendingXp(uint player)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer != null && Lifecycle.GrantPendingXp(player, dbPlayer);
    }

    /// <inheritdoc />
    public bool IsCrafting(uint player)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer?.ActiveCraftSession != null;
    }

    /// <inheritdoc />
    public bool IsActive(uint player, CraftSession session)
    {
        var dbPlayer = _db.Get<Player>(GetObjectUUID(player));
        return dbPlayer != null && CraftSessionLifecycle.IsActive(dbPlayer, session);
    }
}
