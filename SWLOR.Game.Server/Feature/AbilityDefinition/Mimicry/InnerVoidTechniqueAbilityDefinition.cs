using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerVoidTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerVoidTechnique,
                "Inner Void",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.InnerVoid,
                1.3f,
                24f,
                9,
                48,
                30,
                typeof(WeakenedStatusEffect),
                CombatDamageType.Force,
                ResistanceType.Disruption,
                VisualEffect.Vfx_Imp_Negative_Energy,
                maxRange: 12f,
                afterSuccessfulHit: InnateAbility.RestoreFPOnHit(5))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.InnerVoid, 46, 3)
                .MimicryElement(CombatDamageType.Force);

            return _builder.Build();
        }
    }
}
