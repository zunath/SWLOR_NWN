using System;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Abilities;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Cooldowns of the Force Control healing riders. The state lives in <see cref="IForceControlCooldownsService"/>; every
/// member forwards to the container's instance.
/// </summary>
public static class ForceControlCooldowns
{
    private static IForceControlCooldownsService Instance => FacadeAccessor.Resolve<IForceControlCooldownsService>();

    public static bool TryUseForceMend(uint activator, uint target, DateTime now, float cooldownSeconds) =>
        Instance.TryUseForceMend(activator, target, now, cooldownSeconds);

    public static bool TryUseHarmonicRestoration(uint activator, DateTime now, float cooldownSeconds) =>
        Instance.TryUseHarmonicRestoration(activator, now, cooldownSeconds);
}
