using SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility;

namespace SWLOR.Game.Server.Composition
{
    public static partial class GameServices
    {
        /// <summary>What toggle, self status and party status abilities do when they run. Temporary, removed with the rest of <see cref="GameServices"/> in wave C11: the weapon ability base and profile are not converted yet.</summary>
        public static StatusAbilityEffects StatusAbilityEffects => FacadeAccessor.Resolve<StatusAbilityEffects>();

        /// <summary>What a weapon ability profile adds to damage, critical rate and defense ignore. Temporary, removed with the rest of <see cref="GameServices"/> in wave C11: the weapon ability base and profile are not converted yet.</summary>
        public static WeaponAbilityDamageModifiers WeaponAbilityDamageModifiers => FacadeAccessor.Resolve<WeaponAbilityDamageModifiers>();

        /// <summary>The resource and modifier effects of each landed hit of a weapon ability. Temporary, removed with the rest of <see cref="GameServices"/> in wave C11: the weapon ability base and profile are not converted yet.</summary>
        public static WeaponAbilityHitEffects WeaponAbilityHitEffects => FacadeAccessor.Resolve<WeaponAbilityHitEffects>();

        /// <summary>What a weapon ability gives its caster. Temporary, removed with the rest of <see cref="GameServices"/> in wave C11: the weapon ability base and profile are not converted yet.</summary>
        public static WeaponAbilitySelfEffects WeaponAbilitySelfEffects => FacadeAccessor.Resolve<WeaponAbilitySelfEffects>();

        /// <summary>The statuses a landed weapon ability hit adds to its target. Temporary, removed with the rest of <see cref="GameServices"/> in wave C11: the weapon ability base and profile are not converted yet.</summary>
        public static WeaponAbilityStatusRiders WeaponAbilityStatusRiders => FacadeAccessor.Resolve<WeaponAbilityStatusRiders>();

        /// <summary>The temporary stat modifiers a weapon ability profile writes. Temporary, removed with the rest of <see cref="GameServices"/> in wave C11: the weapon ability base and profile are not converted yet.</summary>
        public static WeaponAbilityTemporaryModifiers WeaponAbilityTemporaryModifiers => FacadeAccessor.Resolve<WeaponAbilityTemporaryModifiers>();
    }
}
