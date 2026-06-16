using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

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
                    .Name("Invincible")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds)
                    .UsesAnimation(Animation.ShieldWall),
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
