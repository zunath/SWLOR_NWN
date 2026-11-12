using SWLOR.Game.Server.Core;
using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>
/// When players and beasts died and when a player was last revived mid-fight. The dictionaries that used to be static
/// in <c>Feature.AbilityDefinition.ReviveRules</c> are this instance's own; the rules that read them stay in that
/// class, which passes the time in.
/// </summary>
public class ReviveHistoryService : IReviveHistoryService
{
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<string, DateTime> _lastMidFightReviveAt = new();
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<string, DateTime> _deathAt = new();

    public void RecordDeath(string key, DateTime diedAt)
    {
        _deathAt[key] = diedAt;
    }

    public DateTime? GetDeathTime(string key)
    {
        return _deathAt.TryGetValue(key, out var diedAt) ? diedAt : null;
    }

    public void RecordMidFightRevive(string playerId, DateTime revivedAt)
    {
        _lastMidFightReviveAt[playerId] = revivedAt;
    }

    public DateTime? GetLastMidFightRevive(string playerId)
    {
        return _lastMidFightReviveAt.TryGetValue(playerId, out var revivedAt) ? revivedAt : null;
    }
}
