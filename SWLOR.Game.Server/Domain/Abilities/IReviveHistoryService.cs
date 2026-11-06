using System;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>
/// When players and beasts died and when a player was last revived mid-fight, read by the revive rules. The keys are
/// player UUIDs, or a master's UUID and a beast id for a beast death. Nothing is ever removed, as before.
/// </summary>
public interface IReviveHistoryService
{
    /// <summary>Records when the creature or beast with this key died.</summary>
    void RecordDeath(string key, DateTime diedAt);

    /// <summary>When the creature or beast with this key died, or null when no death was recorded.</summary>
    DateTime? GetDeathTime(string key);

    /// <summary>Records when the player with this UUID was revived while a fight was still going.</summary>
    void RecordMidFightRevive(string playerId, DateTime revivedAt);

    /// <summary>When the player with this UUID was last revived mid-fight, or null when never.</summary>
    DateTime? GetLastMidFightRevive(string playerId);
}
