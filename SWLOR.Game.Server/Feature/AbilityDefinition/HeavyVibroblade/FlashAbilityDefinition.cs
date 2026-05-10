using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class FlashAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            Flash(builder);

            return builder.Build();
        }

        private static void Flash(AbilityBuilder builder)
        {
            builder.Create(FeatType.Flash1, PerkType.Flash)
                .Name("Flash")
                .Level(1)
                .HasActivationDelay(0f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyTelegraphedCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 0, 30, typeof(FlashStatusEffect), CombatImpactAreaShape.Sphere, 0.25f, 5f, centerOnActivator: true);
                    Enmity.ModifyEnmityOnAll(activator, 650);
                })
                .IsCastedAbility()
                .IsHostileAbility()
                .BreaksStealth();
        }
    }
}
