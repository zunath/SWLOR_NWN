using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Spear
{
    public class TotalForceDenialAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            TotalForceDenial1(builder);

            return builder.Build();
        }

        private static void TotalForceDenial1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.TotalForceDenial1, PerkType.TotalForceDenial)
                .Name("Total Force Denial")
                .Level(1)
                .HasActivationDelay(2f)
                .UsesAnimation(Animation.Whirlwind)
                .HasRecastDelay(RecastGroup.TotalForceDenial, 300f)
                .HasImpactAction(TotalForceDenial1ImpactAction)
                .HasTargetingCone(
                    Spell.TotalForceDenial1,
                    5f,
                    5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .SkillType(SkillType.Spear)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(14);
        }

        private static void TotalForceDenial1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.Spear,
                28,
                12,
                typeof(ForceDisruptionStatusEffect),
                CombatImpactAreaShape.Cone,
                0.25f,
                5f,
                5f,
                additionalStatusEffects: new[] { typeof(FoggyMindStatusEffect) });
        }
    }
}
