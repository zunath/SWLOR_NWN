using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    public class RendingBiteAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new AbilityBuilder();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var profile = InnateAbilityProfile.CreaturePhysical;

            _builder
                .Create(FeatType.RendingBite, profile.PlayerPerkType)
                .Name("Rending Bite")
                .HasActivationDelay(1.2f)
                .IsCastedAbility()
                .RequirementStamina(3)
                .HasRecastDelay(RecastGroup.RendingBite, 14f)
                .UsesAnimation(Animation.DoubleStrike)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        location,
                        InnateAbility.ResolveSkillType(activator, profile),
                        14,
                        24,
                        typeof(BleedStatusEffect),
                        false,
                        damageType: CombatDamageType.Physical,
                        statusResistanceType: ResistanceType.Trauma,
                        targetVisualEffect: VisualEffect.Vfx_Com_Chunk_Red_Small,
                        useNPCStatScaling: InnateAbility.ShouldUseNPCStatScaling(activator));
                });

            return _builder.Build();
        }
    }
}
