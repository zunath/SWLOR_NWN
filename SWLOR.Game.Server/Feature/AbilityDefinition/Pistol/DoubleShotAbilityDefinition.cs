using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Pistol
{
    public class DoubleShotAbilityDefinition : IAbilityListDefinition
    {
        private const SkillType Skill = SkillType.Pistol;
        private const int HitCount = 2;

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            DoubleShot1(builder);
            DoubleShot2(builder);
            DoubleShot3(builder);

            return builder.Build();
        }

        private static void DoubleShot1(AbilityBuilder builder)
        {
            DoubleShot(builder, FeatType.DoubleShot1, "Double Shot I", level: 1, stamina: 5);
        }

        private static void DoubleShot2(AbilityBuilder builder)
        {
            DoubleShot(builder, FeatType.DoubleShot2, "Double Shot II", level: 2, stamina: 6);
        }

        private static void DoubleShot3(AbilityBuilder builder)
        {
            DoubleShot(builder, FeatType.DoubleShot3, "Double Shot III", level: 3, stamina: 8);
        }

        private static void DoubleShot(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            int level,
            int stamina)
        {
            builder.Create(feat, PerkType.DoubleShot)
                .Name(name)
                .Level(level)
                .HasActivationDelay(0f)
                .SkillType(Skill)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction(ImpactAction)
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var damage = level switch
            {
                1 => 7,
                2 => 15,
                3 => 24,
                _ => 0
            };

            if (damage <= 0)
                return;

            for (var hit = 0; hit < HitCount; hit++)
            {
                Ability.ApplyCombatImpact(
                    activator,
                    target,
                    targetLocation,
                    Skill,
                    damage,
                    duration: 0,
                    savingThrowDc: 0,
                    SavingThrow.Reflex,
                    statusEffect: null,
                    false);
            }
        }
    }
}
