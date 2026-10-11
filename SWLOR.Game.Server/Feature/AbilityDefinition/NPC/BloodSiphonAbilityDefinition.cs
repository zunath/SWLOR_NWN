using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class BloodSiphonAbilityDefinition : IAbilityListDefinition
    {
        private const int HealingPercent = 50;

        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.BloodSiphon, profile.PlayerPerkType)
                .Name("Blood Siphon")
                .HasActivationDelay(1.1f)
                .IsCastedAbility()
                .RequirementStamina(4)
                .HasRecastDelay(RecastGroup.BloodSiphon, 16f)
                .UsesAnimation(Animation.DoubleStrike)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    using var damageDerivedHealing = Combat.BeginDamageDerivedHealing(activator, target);
                    var damage = Ability.ApplyCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        14,
                        12,
                        typeof(WeakenedStatusEffect),
                        false,
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Trauma,
                        targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));

                    var healed = Combat.ApplyDamageDerivedHealing(activator, damage, HealingPercent, isActivatedHealing: true);
                    if (healed > 0)
                    {
                        var beam = EffectBeam(VisualEffect.Vfx_Beam_Drain, target, BodyNode.Chest);
                        ApplyEffectToObject(DurationType.Temporary, beam, activator, 0.5f);
                    }
                })
                .ExcludedFromMimicry();

            return _builder.Build();
        }
    }
}
