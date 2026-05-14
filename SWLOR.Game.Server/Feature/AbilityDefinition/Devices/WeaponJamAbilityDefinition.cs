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
            WeaponJam2(builder);

            return builder.Build();
        }

        private static void WeaponJam1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WeaponJam1, PerkType.WeaponJam)
                .Name("Weapon Jam I")
                .Level(1)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WeaponJam, 24f)
                .SkillType(SkillType.Devices)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(WeaponJam1ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void WeaponJam2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.WeaponJam2, PerkType.WeaponJam)
                .Name("Weapon Jam II")
                .Level(2)
                .HasActivationDelay(1f)
                .HasRecastDelay(RecastGroup.WeaponJam, 24f)
                .SkillType(SkillType.Devices)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(WeaponJam2ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(5);
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

        private static void WeaponJam2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Devices,
                0,
                18,
                typeof(WeaponJam2StatusEffect),
                false,
                Array.Empty<Type>(),
                damageType: CombatDamageType.Electrical,
                targetVisualEffect: VisualEffect.Vfx_Com_Hit_Electrical);
        }
    }
}
