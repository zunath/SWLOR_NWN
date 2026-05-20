using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class EarthshatterAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Earthshatter1(builder);

            return builder.Build();
        }

        private static void Earthshatter1(AbilityBuilder builder)
        {
            builder
                .Create(FeatType.Earthshatter1, PerkType.Earthshatter)
                .Name("Earthshatter")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.Earthshatter, 90f)
                .HasImpactAction(Earthshatter1ImpactAction)
                .HasTargetingLine(
                    Spell.Earthshatter1,
                    8f,
                    2.5f,
                    AbilityTargetingFlags.HarmsEnemies | AbilityTargetingFlags.OriginOnSelf)
                .SkillType(SkillType.HeavyVibroblade)
                .IsCastedAbility()
                .IsHostileAbility()
                .IsAreaAbility()
                .BreaksStealth()
                .RequirementStamina(12);
        }

        private static void Earthshatter1ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.HeavyVibroblade,
                20,
                12,
                null,
                CombatImpactAreaShape.Line,
                0.25f,
                8f,
                2.5f,
                statusEffectFactory: () => new ForceDisruptionStatusEffect(true));
        }
    }
}
