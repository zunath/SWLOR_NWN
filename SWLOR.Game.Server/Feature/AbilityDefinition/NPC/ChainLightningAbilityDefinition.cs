using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ChainLightningAbilityDefinition : IAbilityListDefinition
    {
        private const int MaxArcs = 2;
        private const float ArcRadius = 5f;
        private const int ArcDamage = 12;
        private const int ShockDuration = 12;

        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            InnateAbility.BuildSingleTarget(
                _builder,
                FeatType.ChainLightning,
                "Chain Lightning",
                Animation.CastOutAnimation,
                InnateAbilityProfile.Force,
                RecastGroup.ChainLightning,
                1.3f,
                22f,
                6,
                16,
                ShockDuration,
                typeof(ShockStatusEffect),
                CombatDamageType.Electrical,
                ResistanceType.Electrical,
                VisualEffect.Vfx_Imp_Lightning_S,
                maxRange: 10f,
                afterSuccessfulHit: ArcLightning);

            return _builder.Build();
        }

        private static void ArcLightning(uint activator, uint target)
        {
            ApplyLightningBeam(activator, BodyNode.Hand, target);

            foreach (var arc in AbilityTargeting.GetHostileTargetsNearLocation(
                         activator,
                         GetLocation(target),
                         ArcRadius,
                         MaxArcs,
                         predicate: creature => creature != target))
            {
                ApplyLightningBeam(target, BodyNode.Chest, arc);
                Ability.ApplyCombatImpact(
                    activator,
                    arc,
                    GetLocation(arc),
                    InnateAbility.ResolveSkillType(activator, InnateAbilityProfile.Force),
                    ArcDamage,
                    ShockDuration,
                    typeof(ShockStatusEffect),
                    false,
                    damageType: CombatDamageType.Electrical,
                    statusResistanceType: ResistanceType.Electrical,
                    targetVisualEffect: VisualEffect.Vfx_Imp_Lightning_S,
                    playImpactAnimation: false,
                    useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
            }
        }

        private static void ApplyLightningBeam(uint source, BodyNode sourceNode, uint target)
        {
            var beam = EffectBeam(VisualEffect.Vfx_Beam_Lightning, source, sourceNode);
            AssignCommand(source, () => ApplyEffectToObject(DurationType.Temporary, beam, target, 0.3f));
        }
    }
}
