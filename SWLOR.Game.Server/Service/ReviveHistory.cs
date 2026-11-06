using System;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// When players and beasts died and when a player was last revived mid-fight. The state lives in
/// <see cref="IReviveHistoryService"/>; every member forwards to the container's instance.
/// </summary>
public static class ReviveHistory
{
    private static IReviveHistoryService Instance => FacadeAccessor.Resolve<IReviveHistoryService>();

    public static void RecordDeath(string key, DateTime diedAt) => Instance.RecordDeath(key, diedAt);

    public static DateTime? GetDeathTime(string key) => Instance.GetDeathTime(key);

    public static void RecordMidFightRevive(string playerId, DateTime revivedAt) => Instance.RecordMidFightRevive(playerId, revivedAt);

    public static DateTime? GetLastMidFightRevive(string playerId) => Instance.GetLastMidFightRevive(playerId);
}
