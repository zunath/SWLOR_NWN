using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class SoulBurstAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            SoulBurst1(builder);

            return builder.Build();
        }

        private static void SoulBurst1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.SoulBurst1, PerkType.SoulBurst)
                .Name("Soul Burst")
                .Level(1)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.CastOutAnimation)
                .HasRecastDelay(RecastGroup.SoulBurst, 60f)
                .HasTargetingCone(
                    Spell.SoulBurst1,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .HasImpactAction(SoulBurst1ImpactAction)
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void SoulBurst1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            SacrificeHitPoints(activator, 40, 10);
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.HeavyVibroblade,
                35,
                0,
                null,
                CombatImpactAreaShape.Cone,
                0.25f,
                5f,
                5f,
                afterSuccessfulHit: hitTarget => ApplyEssenceHunter(activator, hitTarget));
        }
    }
}
