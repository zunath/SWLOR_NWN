using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class ShieldBashAbilityDefinition : IAbilityListDefinition
    {
        private const string ReplacementAnimationName = "Shield_Bash";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureShieldBash(builder, FeatType.ShieldBash1, "Shield Bash I", 1, 4, 3);
            ConfigureShieldBash(builder, FeatType.ShieldBash2, "Shield Bash II", 2, 6, 5);
            ConfigureShieldBash(builder, FeatType.ShieldBash3, "Shield Bash III", 3, 8, 6);
            ConfigureShieldBash(builder, FeatType.ShieldBash4, "Shield Bash IV", 4, 10, 8);

            return builder.Build();
        }

        private static void ConfigureShieldBash(
            AbilityBuilder builder,
            FeatType featType,
            string name,
            int level,
            int physicalDefensePercent,
            int stamina)
        {
            builder
                .Create(featType, PerkType.ShieldBash)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.ShieldBash, 20f)
                .SkillType(SkillType.Vibroblade)
                .IsSingleTargetAbility()
                .HasImpactAction((activator, target, effectivePerkLevel, targetLocation) =>
                    ApplyShieldBash(activator, target, targetLocation, physicalDefensePercent))
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .UsesImpactAnimationOverwrite(ReplacementAnimationName)
                .RequirementStamina(stamina);
        }

        private static void ApplyShieldBash(
            uint activator,
            uint target,
            Location targetLocation,
            int physicalDefensePercent)
        {
            var totalDamage = Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Vibroblade,
                0,
                0,
                null,
                false);

            if (totalDamage <= 0)
                return;

            var physicalDefense = Stat.GetDefense(
                activator,
                CombatDamageType.Physical,
                AbilityType.Vitality);
            var bonusDamage = (int)Math.Ceiling(physicalDefense * (physicalDefensePercent / 100f));
            if (bonusDamage <= 0)
                return;

            Combat.ApplyTriggeredDamage(
                activator,
                target,
                bonusDamage,
                CombatDamageType.Physical,
                SkillType.Vibroblade);
        }
    }
}
