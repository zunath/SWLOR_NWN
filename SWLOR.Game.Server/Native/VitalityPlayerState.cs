using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Native;

/// <summary>Caches player Vitality between native attribute updates, until the stats are destroyed.</summary>
internal sealed class VitalityPlayerState
{
    public CNWSCreature Creature { get; }
    public CNWSCreatureStats Stats { get; }
    public int Score { get; set; }
    public HashSet<ulong> EffectIds { get; } = new();

    public VitalityPlayerState(CNWSCreature creature, CNWSCreatureStats stats)
    {
        Creature = creature;
        Stats = stats;
        Score = stats.GetCONStat();
        RefreshEffects();
    }

    public void RefreshEffects()
    {
        EffectIds.Clear();
        foreach (var effect in Creature.m_appliedEffects)
            if ((effect.m_nType == (ushort)EffectTypeEngine.AbilityIncrease ||
                 effect.m_nType == (ushort)EffectTypeEngine.AbilityDecrease) &&
                effect.GetInteger(0) == (int)AbilityType.Vitality)
                EffectIds.Add(effect.m_nID);
    }
}
