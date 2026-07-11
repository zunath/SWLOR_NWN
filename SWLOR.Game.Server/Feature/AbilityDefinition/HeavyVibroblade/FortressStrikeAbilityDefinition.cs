using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class FortressStrikeAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FortressStrike1(builder);
            FortressStrike2(builder);
            FortressStrike3(builder);

            return builder.Build();
        }

        private static void FortressStrike1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FortressStrike1, PerkType.FortressStrike)
                .Name("Fortress Strike I")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FortressStrike, 18f)
                .HasImpactAction(FortressStrike1ImpactAction)
                .SkillType(SkillType.HeavyVibroblade)
                .IsWeaponAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .BreaksStealth()
                .RequirementStamina(4);
        }

        private static void FortressStrike2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FortressStrike2, PerkType.FortressStrike)
                .Name("Fortress Strike II")
                .Level(2)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FortressStrike, 18f)
                .HasImpactAction(FortressStrike2ImpactAction)
                .SkillType(SkillType.HeavyVibroblade)
                .IsWeaponAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .BreaksStealth()
                .RequirementStamina(7);
        }

        private static void FortressStrike3(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FortressStrike3, PerkType.FortressStrike)
                .Name("Fortress Strike III")
                .Level(3)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.FortressStrike, 18f)
                .HasImpactAction(FortressStrike3ImpactAction)
                .SkillType(SkillType.HeavyVibroblade)
                .IsWeaponAbility()
                .IsHostileAbility()
                .IsSingleTargetAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void FortressStrike1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyFortressStrike(activator, target, targetLocation, 10, 10, 350);
        }

        private static void FortressStrike2ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyFortressStrike(activator, target, targetLocation, 20, 20, 450);
        }

        private static void FortressStrike3ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            ApplyFortressStrike(activator, target, targetLocation, 30, 30, 550);
        }

        private static void ApplyFortressStrike(
            uint activator,
            uint target,
            Location targetLocation,
            int damageBonus,
            int defensePercent,
            int enmityBonus)
        {
            var damage = Ability.ApplyCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.HeavyVibroblade,
                damageBonus,
                0,
                null,
                false);

            Enmity.ModifyEnmity(activator, target, enmityBonus + damage);
            StatusEffect.ApplyStatusEffect(activator, activator, new FortressStrikeStatusEffect(defensePercent), 30f);
        }
    }
}
