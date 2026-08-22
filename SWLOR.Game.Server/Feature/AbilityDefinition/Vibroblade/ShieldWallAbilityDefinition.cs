using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Vibroblade
{
    public class ShieldWallAbilityDefinition : WeaponActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const string ReplacementAnimationName = "Shield_Wall";
        private const float ChannelSeconds = 30f;

        // The damage reduction only lasts while channeling, so the status runs for the channel itself
        // and is removed early if the channel is interrupted.
        private const float DurationSeconds = ChannelSeconds;
        private const int StaminaCost = 10;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureShieldWall(builder, FeatType.ShieldWall1, "Shield Wall I", 1, 20);
            ConfigureShieldWall(builder, FeatType.ShieldWall2, "Shield Wall II", 2, 35);

            return builder.Build();
        }

        private static void ConfigureShieldWall(
            AbilityBuilder builder,
            FeatType featType,
            string name,
            int level,
            int damageReductionPercent)
        {
            ConfigurePartyStatus(
                builder
                    .Create(featType, PerkType.ShieldWall)
                    .Name(name)
                    .Level(level)
                    .HasRecastDelay(RecastGroup.ShieldWall, 45f)
                    .UsesAnimationOverwrite(ReplacementAnimationName)
                    .IsChanneledAbility(activator => StatusEffect.RemoveStatusEffectFromAllTargetsBySource(
                        typeof(ShieldWallStatusEffect),
                        activator,
                        false)),
                () => new ShieldWallStatusEffect(damageReductionPercent),
                DurationSeconds,
                StaminaCost,
                true,
                ChannelSeconds);
        }
    }
}
