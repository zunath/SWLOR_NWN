using System.Collections.Generic;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.HeavyVibroblade
{
    public class EssenceHunterAbilityDefinition : HeavyVibrobladeActiveAbilityDefinitionBase, IAbilityListDefinition
    {
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            var builder = new AbilityBuilder();

            EssenceHunter(builder);

            return builder.Build();
        }

        private static void EssenceHunter(AbilityBuilder builder)
        {
            builder.Create(FeatType.EssenceHunter1, PerkType.EssenceHunter)
                .Name("Essence Hunter")
                .Level(1)
                .HasActivationDelay(0f)
                .HasRecastDelay(RecastGroup.EssenceHunter, 45f)
                .HasImpactAction((activator, target, level, targetLocation) =>
                {
                    Ability.ApplyCombatImpact(activator, target, targetLocation, SkillType.HeavyVibroblade, 18, 12, 15, SavingThrow.Fortitude, typeof(EssenceDrainStatusEffect), false);
                })
                .IsWeaponAbility()
                .IsHostileAbility()
                .BreaksStealth()
                .RequirementStamina(6);
        }
    }
}
