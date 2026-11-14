using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class ToxicSpitAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.ToxicSpit, profile.PlayerPerkType)
                .Name("Toxic Spit")
                .HasActivationDelay(1.0f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasRecastDelay(RecastGroup.ToxicSpit, 18f)
                .UsesAnimation(Animation.CastOutAnimation)
                .IsSingleTargetAbility()
                .HasMaxRange(8f)
                .RequiresTarget()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        8,
                        30,
                        typeof(PoisonStatusEffect),
                        false,
                        damageType: CombatDamageType.Poison,
                        statusResistanceType: ResistanceType.Poison,
                        targetVisualEffect: VisualEffect.Vfx_Imp_Poison_S,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
