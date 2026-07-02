using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Devices
{
    public sealed class WeaponJamAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            WeaponJam1(builder);

            return builder.Build();
        }

        private static void WeaponJam1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WeaponJam1, PerkType.WeaponJam)
                .Name("Weapon Jam")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WeaponJam, 15f)
                .SkillType(SkillType.Devices)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.CastOutAnimation)
                .HasMaxRange(DeviceAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(WeaponJam1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void WeaponJam1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                0,
                18,
                typeof(WeaponJam1StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical);
        }
    }
}
