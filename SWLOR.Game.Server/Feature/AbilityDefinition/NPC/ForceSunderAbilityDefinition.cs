using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ForceSunderAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.ForceSunder,
                "Force Sunder",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Force,
                RecastGroup.ForceSunder,
                1.3f,
                18f,
                6,
                16,
                14,
                typeof(ForceErosionStatusEffect),
                CombatDamageType.Force,
                ResistanceType.Disruption,
                maxRange: 8f,
                afterSuccessfulHit: ApplyForceSunderBeam);

            return _builder.Build();
        }

        private static void ApplyForceSunderBeam(uint activator, uint target)
        {
            var drainBeam = EffectBeam(VisualEffect.Vfx_Beam_Drain, activator, BodyNode.Hand);
            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Temporary, drainBeam, target, 1.5f));
        }
    }
}
