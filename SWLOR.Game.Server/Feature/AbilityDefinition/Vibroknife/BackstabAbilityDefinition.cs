using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife
{
    public class BackstabAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab1, PerkType.Backstab)
                    .Name("Backstab I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Backstab, 60f)
                    .UsesAnimation(Animation.Backstab)
                    .PlaysSoundOnImpact("cb_sw_blade1")
                    .HasCustomValidation(ValidateBehindTarget),
                SkillType.Vibroknife,
                20,
                3);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab2, PerkType.Backstab)
                    .Name("Backstab II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.Backstab, 60f)
                    .UsesAnimation(Animation.Backstab)
                    .PlaysSoundOnImpact("cb_sw_blade1")
                    .HasCustomValidation(ValidateBehindTarget),
                SkillType.Vibroknife,
                40,
                5);
            ConfigureCastedTarget(
                builder
                    .Create(FeatType.Backstab3, PerkType.Backstab)
                    .Name("Backstab III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.Backstab, 60f)
                    .UsesAnimation(Animation.Backstab)
                    .PlaysSoundOnImpact("cb_sw_blade1")
                    .HasCustomValidation(ValidateBehindTarget),
                SkillType.Vibroknife,
                60,
                8,
                3,
                typeof(KnockdownStatusEffect));

            return builder.Build();
        }

        private static string ValidateBehindTarget(uint activator, uint target, int level, Location targetLocation)
        {
            return Combat.IsTargetNotFacingAttacker(activator, target)
                ? string.Empty
                : "You must be behind your target.";
        }
    }
}
