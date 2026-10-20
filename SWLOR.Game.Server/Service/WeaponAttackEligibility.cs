using System.Collections.Generic;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service;

/// <summary>Weapon classes can require valid equipment without coupling shared attacks to their implementation.</summary>
public static class WeaponAttackEligibility
{
    private static readonly Dictionary<BaseItem, Func<uint, uint, WeaponAttackPhase, bool>> Rules = new();

    public static void RegisterRule(BaseItem type, Func<uint, uint, WeaponAttackPhase, bool> rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        Rules[type] = rule;
    }

    public static bool IsAllowed(uint creature, uint weapon, WeaponAttackPhase phase) =>
        !GetIsObjectValid(weapon) || IsAllowed(GetBaseItemType(weapon), creature, weapon, phase);

    public static bool IsAllowed(BaseItem type, uint creature, uint weapon, WeaponAttackPhase phase) =>
        !Rules.TryGetValue(type, out var rule) || rule(creature, weapon, phase);
}
