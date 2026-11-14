using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Piloting
{
    /// <summary>
    /// Invincible: for 45 s the ship takes 50% less damage and is immune to Knockdown and Dazed. Ported from the
    /// Vibroblade Invincible (the status, duration, STM and cooldown are the ground's). The immunities end when
    /// the pilot leaves space.
    /// </summary>
    public class InvincibleAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const float ActiveSeconds = 45f;
        private const int Stamina = 15;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureSelfStatus(
                builder.Create(FeatType.BulwarkInvincible, PerkType.BulwarkInvincible)
                    .Name("Invincible")
                    .Level(1)
                    .SkillType(SkillType.Piloting)
                    .CanBeUsedInSpace()
                    .HasRecastDelay(RecastGroup.Capstone, 90f),
                typeof(InvincibleStatusEffect),
                ActiveSeconds,
                Stamina,
                activator =>
                {
                    Ability.ApplyTemporaryImmunity(activator, ActiveSeconds, ImmunityType.Knockdown);
                    Ability.ApplyTemporaryImmunity(activator, ActiveSeconds, ImmunityType.Dazed);
                });

            return builder.Build();
        }
    }
}
