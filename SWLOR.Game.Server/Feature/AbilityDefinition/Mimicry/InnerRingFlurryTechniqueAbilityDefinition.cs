using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class InnerRingFlurryTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.InnerRingFlurryTechnique,
                "Inner Ring Flurry",
                Animation.CrossCut,
                InnateAbilityProfile.Mimicry,
                RecastGroup.InnerRingFlurry,
                1.3f,
                24f,
                9,
                0,
                30,
                typeof(BleedStatusEffect),
                CombatDamageType.Physical,
                ResistanceType.Trauma,
                VisualEffect.Vfx_Com_Special_Red_White,
                maxRange: 3f,
                afterSuccessfulHit: InnateAbility.RestoreStaminaOnHit(4))
                .SkillType(SkillType.Mimicry)
                .Level(1)
                .CombatImpactDamageAbility(AbilityType.Agility)
                .MimicryTechnique(FeatType.InnerRingFlurry, 44, 3)
                .MimicryElement(CombatDamageType.Physical);

            return _builder.Build();
        }
    }
}
