using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class CreepingTerrorAbilityDefinition : IAbilityListDefinition
    {

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            CreepingTerror1(builder);
            CreepingTerror2(builder);
            CreepingTerror3(builder);

            return builder.Build();
        }

        private static void CreepingTerror1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror1, PerkType.CreepingTerror)
                .Name("Creeping Terror I")
                .Level(1)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var statusEffectService = App.Resolve<IStatusEffectService>();
                    statusEffectService.Apply(activator, target, StatusEffectType.CreepingTerror, 24f, 1);
                });
        }

        private static void CreepingTerror2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror2, PerkType.CreepingTerror)
                .Name("Creeping Terror II")
                .Level(2)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var statusEffectService = App.Resolve<IStatusEffectService>();
                    statusEffectService.Apply(activator, target, StatusEffectType.CreepingTerror, 24f, 2);
                });
        }

        private static void CreepingTerror3(IAbilityBuilder builder)
        {
            builder.Create(FeatType.CreepingTerror3, PerkType.CreepingTerror)
                .Name("Creeping Terror III")
                .Level(3)
                .HasRecastDelay(RecastGroup.CreepingTerror, 30f)
                .HasActivationDelay(2f)
                .RequirementFP(8)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .IsHostileAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var statusEffectService = App.Resolve<IStatusEffectService>();
                    statusEffectService.Apply(activator, target, StatusEffectType.CreepingTerror, 24f, 3);
                });
        }
    }
}
