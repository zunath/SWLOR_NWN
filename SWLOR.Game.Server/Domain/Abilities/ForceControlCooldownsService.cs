using SWLOR.Game.Server.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>
/// Cooldowns of the Force Control healing riders. The dictionaries and the cooldown check that used to be static in
/// <c>Feature.AbilityDefinition.Force.ForceControlHealingEffects</c> are moved here unchanged; the class passes in the
/// time it read.
/// </summary>
public class ForceControlCooldownsService : IForceControlCooldownsService
{
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<(uint, uint), DateTime> _forceMendCooldowns = new();
    [ReloadState(ReloadStateKind.Carried)]
    private readonly Dictionary<uint, DateTime> _harmonicRestorationCooldowns = new();

    public bool TryUseForceMend(uint activator, uint target, DateTime now, float cooldownSeconds)
    {
        var key = (activator, target);
        return TryUseCooldown(_forceMendCooldowns, key, now, cooldownSeconds);
    }

    public bool TryUseHarmonicRestoration(uint activator, DateTime now, float cooldownSeconds)
    {
        return TryUseCooldown(_harmonicRestorationCooldowns, activator, now, cooldownSeconds);
    }

    private static bool TryUseCooldown<TKey>(
        Dictionary<TKey, DateTime> cooldowns,
        TKey key,
        DateTime now,
        float cooldownSeconds)
        where TKey : notnull
    {
        foreach (var expired in cooldowns.Where(x => x.Value <= now).Select(x => x.Key).ToList())
        {
            cooldowns.Remove(expired);
        }

        if (cooldowns.TryGetValue(key, out var nextAvailable) && nextAvailable > now)
            return false;

        cooldowns[key] = now.AddSeconds(cooldownSeconds);
        return true;
    }
}
