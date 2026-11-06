using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service;

/// <summary>
/// Weapon classes can require valid equipment without coupling shared attacks to their implementation. The logic lives
/// in <see cref="IWeaponAttackEligibilityService"/>; every member forwards to the container's instance.
/// </summary>
public static class WeaponAttackEligibility
{
    private static IWeaponAttackEligibilityService Instance => FacadeAccessor.Resolve<IWeaponAttackEligibilityService>();

    public static void RegisterRule(BaseItem type, Func<uint, uint, WeaponAttackPhase, bool> rule) =>
        Instance.RegisterRule(type, rule);

    public static bool IsAllowed(uint creature, uint weapon, WeaponAttackPhase phase) =>
        Instance.IsAllowed(creature, weapon, phase);

    public static bool IsAllowed(BaseItem type, uint creature, uint weapon, WeaponAttackPhase phase) =>
        Instance.IsAllowed(type, creature, weapon, phase);
}
