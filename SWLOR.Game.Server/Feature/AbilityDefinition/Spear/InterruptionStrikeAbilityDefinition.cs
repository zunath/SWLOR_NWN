using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class InterruptionStrikeAbilityDefinition : SpearActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        private const int InterruptionStrike1Damage = 0;
        private const int InterruptionStrike2Damage = 20;
        private const int FoggyMindDurationSeconds = 30;
        private const int FoggyMindActivationDelaySeconds = 2;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCurrentAttackTargetInterrupt(
                builder
                    .Create(FeatType.InterruptionStrike1, PerkType.InterruptionStrike)
                    .Name("Interruption Strike I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.InterruptionStrike, 30f)
                    .UsesAnimation(Animation.DoubleThrust),
                SkillType.Spear,
                InterruptionStrike1Damage,
                FoggyMindDurationSeconds,
                typeof(FoggyMindStatusEffect),
                5,
                FoggyMind(FoggyMindActivationDelaySeconds));
            ConfigureCurrentAttackTargetInterrupt(
                builder
                    .Create(FeatType.InterruptionStrike2, PerkType.InterruptionStrike)
                    .Name("Interruption Strike II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.InterruptionStrike, 30f)
                    .UsesAnimation(Animation.DoubleThrust),
                SkillType.Spear,
                InterruptionStrike2Damage,
                FoggyMindDurationSeconds,
                typeof(FoggyMindStatusEffect),
                8,
                FoggyMind(FoggyMindActivationDelaySeconds));

            return builder.Build();
        }
    }
}
