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
                .HasRecastDelay(RecastGroup.FortressStrike, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
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
                .HasRecastDelay(RecastGroup.FortressStrike, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
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
                .HasRecastDelay(RecastGroup.FortressStrike, 30f)
                .HasImpactAction(ImpactAction)
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(10);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            switch (level)
            {
                case 1:
                    ApplyFortressStrike(activator, target, targetLocation, 10, 10);
                    break;
                case 2:
                    ApplyFortressStrike(activator, target, targetLocation, 20, 20);
                    break;
                case 3:
                    ApplyFortressStrike(activator, target, targetLocation, 30, 30);
                    break;
            }
        }

        private static void ApplyFortressStrike(
            uint activator,
            uint target,
            Location targetLocation,
            int damageBonus,
            int defensePercent)
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

            Enmity.ModifyEnmity(activator, target, 350 + damage);
            StatusEffect.ApplyStatusEffect(activator, activator, new FortressStrikeStatusEffect(defensePercent), 16f);
        }
    }
}
