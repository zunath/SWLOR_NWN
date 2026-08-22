using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
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
            Earthshatter2(builder);

            return builder.Build();
        }

        private static void Earthshatter1(AbilityBuilder builder)
        {
            BuildEarthshatter(builder, FeatType.Earthshatter1, Spell.Earthshatter1, "Earthshatter I", 1);
        }

        private static void Earthshatter2(AbilityBuilder builder)
        {
            BuildEarthshatter(builder, FeatType.Earthshatter2, Spell.Earthshatter2, "Earthshatter II", 2);
        }

        private static void BuildEarthshatter(
            AbilityBuilder builder,
            FeatType feat,
            Spell spell,
            string name,
            int level)
        {
            builder
                .Create(feat, PerkType.Earthshatter)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .UsesAnimation(Animation.DoubleThrust)
                .HasRecastDelay(RecastGroup.Earthshatter, 45f)
                .HasImpactAction(EarthshatterImpactAction)
                .HasTargetingLine(
                    spell,
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

        private static void EarthshatterImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            Ability.ApplyTelegraphedCombatImpact(
                activator,
                target,
                targetLocation,
                SkillType.HeavyVibroblade,
                20,
                18,
                null,
                CombatImpactAreaShape.Line,
                0.25f,
                8f,
                2.5f,
                statusEffectFactory: () => new ForceDisruptionStatusEffect(),
                baseDamageAdjustment: _ => Stat.GetStatAdjustment(activator, StatType.EarthshatterDamageBonus),
                enmityBonus: Stat.GetStatAdjustment(activator, StatType.EarthshatterEnmityBonus));
        }
    }
}
