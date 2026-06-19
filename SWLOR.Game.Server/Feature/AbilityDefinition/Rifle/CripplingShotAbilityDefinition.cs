using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Rifle
{
    public class CripplingShotAbilityDefinition : IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            ConfigureCripplingShot(
                builder
                    .Create(FeatType.CripplingShot1, PerkType.CripplingShot)
                    .Name("Crippling Shot I")
                    .Level(1)
                    .HasRecastDelay(RecastGroup.CripplingShot, 30f),
                12,
                12,
                4);
            ConfigureCripplingShot(
                builder
                    .Create(FeatType.CripplingShot2, PerkType.CripplingShot)
                    .Name("Crippling Shot II")
                    .Level(2)
                    .HasRecastDelay(RecastGroup.CripplingShot, 30f),
                22,
                15,
                6);
            ConfigureCripplingShot(
                builder
                    .Create(FeatType.CripplingShot3, PerkType.CripplingShot)
                    .Name("Crippling Shot III")
                    .Level(3)
                    .HasRecastDelay(RecastGroup.CripplingShot, 30f),
                34,
                20,
                8);

            return builder.Build();
        }

        private static void ConfigureCripplingShot(
            AbilityBuilder ability,
            int baseDamage,
            int duration,
            int stamina)
        {
            ability
                .HasActivationDelay(0f)
                .SkillType(SkillType.Rifle)
                .CombatImpactDamageAbility(AbilityType.Perception)
                .UsesImpactAnimation(Animation.PointPistol)
                .HasMaxRange(RifleAbilityRange.Standard)
                .IsSingleTargetAbility()
                .RequiresTarget()
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyCombatImpact(
                        activator,
                        target,
                        targetLocation,
                        SkillType.Rifle,
                        baseDamage,
                        duration,
                        typeof(DisorientedStatusEffect),
                        false,
                        combatImpactDamageAbility: AbilityType.Perception);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(stamina);
        }
    }
}
