using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>What toggle, self status and party status abilities do when they run. Temporary: the weapon ability base reaches the part here until it is converted.</summary>
        public static StatusAbilityEffects StatusAbilityEffects => FacadeAccessor.Resolve<StatusAbilityEffects>();

        /// <summary>The activation step of a weapon ability. Temporary: the weapon ability base reaches the part here until it is converted.</summary>
        public static WeaponAbilityActivations WeaponAbilityActivations => FacadeAccessor.Resolve<WeaponAbilityActivations>();

        /// <summary>Friendly and self-status weapon abilities and their target validation. Temporary: the weapon ability base reaches the part here until it is converted.</summary>
        public static WeaponAbilityFriendlyTargets WeaponAbilityFriendlyTargets => FacadeAccessor.Resolve<WeaponAbilityFriendlyTargets>();

        /// <summary>The hostile impact of a weapon ability. Temporary: the weapon ability base reaches the part here until it is converted.</summary>
        public static WeaponAbilityImpacts WeaponAbilityImpacts => FacadeAccessor.Resolve<WeaponAbilityImpacts>();

        /// <summary>The statuses a landed weapon ability hit adds to its target. Temporary: the status spread snapshot, a data type the container does not build, reads the source statuses through the part until it is converted.</summary>
        public static WeaponAbilityStatusRiders WeaponAbilityStatusRiders => FacadeAccessor.Resolve<WeaponAbilityStatusRiders>();
    }
}
