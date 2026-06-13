using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public sealed class FuryStanceAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            FuryStance1(builder);
            FuryStance2(builder);

            return builder.Build();
        }

        private static void FuryStance1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FuryStance1, PerkType.FuryStance)
                .Name("Fury Stance I")
                .Level(1)
                .HasRecastDelay(RecastGroup.FuryStance, 60f)
                .SkillType(SkillType.Force)
                .RequirementFP(5);

            ConfigureToggle(builder, typeof(FuryStance1StatusEffect));
        }

        private static void FuryStance2(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.FuryStance2, PerkType.FuryStance)
                .Name("Fury Stance II")
                .Level(2)
                .HasRecastDelay(RecastGroup.FuryStance, 60f)
                .SkillType(SkillType.Force)
                .RequirementFP(8);

            ConfigureToggle(builder, typeof(FuryStance2StatusEffect));
        }
    }
}
