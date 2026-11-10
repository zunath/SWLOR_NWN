using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Domain.Foundation;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class InvincibleAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder
                    .Create(FeatType.Invincible1, PerkType.Invincible)
                    .DisplaysVisualEffectOnSuccessfulImpact(VisualEffect.Vfx_Ability_Invincible)
                    .Name("Invincible")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                    .UsesAnimation(AuthoredAnimation.Invincible),
                typeof(InvincibleStatusEffect),
                CapstoneAbility.ActiveDurationSeconds,
                CapstoneAbility.StaminaCost,
                activator =>
                {
                    Ability.ApplyTemporaryImmunity(activator, CapstoneAbility.ActiveDurationSeconds, ImmunityType.Knockdown);
                    Ability.ApplyTemporaryImmunity(activator, CapstoneAbility.ActiveDurationSeconds, ImmunityType.Dazed);
                },
                activationDelay: 1f);

            return builder.Build();
        }
    }
}
