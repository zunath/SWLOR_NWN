using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class HoldfastSlamTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.HoldfastSlamTechnique,
                "Holdfast Slam",
                Animation.DoubleThrust,
                InnateAbilityProfile.Mimicry,
                RecastGroup.HoldfastSlam,
                1.3f,
                24f,
                9,
                0,
                30,
                typeof(SunderStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Chunk_Red_Medium,
                maxRange: 3f,
                enmityBonus: 100,
                additionalStatusEffects: new[] { typeof(ExposedStatusEffect) })
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Vitality)
                .MimicryTechnique(FeatType.HoldfastSlam, 44, 3)
                .MimicryElement(CombatDamageType.Physical);

            return _builder.Build();
        }
    }
}
