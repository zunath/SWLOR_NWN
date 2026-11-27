using System;

namespace SWLOR.Game.Server.Domain.Abilities;

/// <summary>Cooldowns of the Force Control healing riders (Force Mend and Harmonic Restoration).</summary>
public interface IForceControlCooldownsService
{
    /// <summary>
    /// Starts the Force Mend cooldown for this caster and target and returns true, or returns false while the previous
    /// one is still running. Cooldowns that ended by <paramref name="now"/> are dropped first.
    /// </summary>
    bool TryUseForceMend(uint activator, uint target, DateTime now, float cooldownSeconds);

    /// <summary>
    /// Starts the Harmonic Restoration cooldown for this caster and returns true, or returns false while the previous
    /// one is still running. Cooldowns that ended by <paramref name="now"/> are dropped first.
    /// </summary>
    bool TryUseHarmonicRestoration(uint activator, DateTime now, float cooldownSeconds);
}
