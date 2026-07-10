using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class GlacialSlimeTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.GlacialSlimeTechnique,
                "Glacial Slime Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.GlacialSlime,
                1.2f,
                15f,
                5,
                16,
                10,
                typeof(FreezingStatusEffect),
                CombatDamageType.Ice,
                ResistanceType.Ice,
                VisualEffect.Vfx_Imp_Frost_S,
                maxRange: 8f)
                .SkillType(SkillType.Mimicry)
                .Level(2)
                .CombatImpactDamageAbility(AbilityType.Might)
                .MimicryTechnique(FeatType.GlacialSlime, 2, 2);

            return _builder.Build();
        }
    }
}
