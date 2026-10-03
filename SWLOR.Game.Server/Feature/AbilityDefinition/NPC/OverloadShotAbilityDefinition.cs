using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class OverloadShotAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.OverloadShot,
                "Overload Shot",
                Animation.PointPistol,
                InnateAbilityProfile.Rifle,
                RecastGroup.OverloadShot,
                1.2f,
                17f,
                5,
                20,
                12,
                typeof(ShockStatusEffect),
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                maxRange: 12f,
                afterSuccessfulHit: ApplyOverloadShotBeam);

            return _builder.Build();
        }

        private static void ApplyOverloadShotBeam(uint activator, uint target)
        {
            var beam = EffectBeam(VisualEffect.Vfx_Beam_Lightning, activator, BodyNode.Hand);
            AssignCommand(activator, () => ApplyEffectToObject(DurationType.Temporary, beam, target, 0.3f));
        }
    }
}
