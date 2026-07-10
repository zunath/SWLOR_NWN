using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition.NPC;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry
{
    public class ForceSunderTechniqueAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.ForceSunderTechnique,
                "Force Sunder Technique",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Mimicry,
                RecastGroup.ForceSunder,
                1.3f,
                18f,
                7,
                24,
                14,
                typeof(ForceErosionStatusEffect),
                CombatDamageType.Force,
                ResistanceType.Disruption,
                maxRange: 8f,
                afterSuccessfulHit: ApplyForceSunderBeam)
                .SkillType(SkillType.Mimicry)
                .Level(3)
                .CombatImpactDamageAbility(AbilityType.Willpower)
                .MimicryTechnique(FeatType.ForceSunder, 3, 2);

            return _builder.Build();
        }

        private static void ApplyForceSunderBeam(uint activator, uint target)
        {
            var drainBeam = EffectBeam(VisualEffect.Vfx_Beam_Drain, activator, BodyNode.Hand);
            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Temporary, drainBeam, target, 1.5f));
        }
    }
}
