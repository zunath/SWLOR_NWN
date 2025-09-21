using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityServicex;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;
using SWLOR.Shared.Core.Models;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceValorAbilityDefinition : IAbilityListDefinition
    {
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IStatusEffectService _statusEffectService;

        public ForceValorAbilityDefinition(ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities(IAbilityBuilder builder)
        {
            ForceValor1(builder);
            ForceValor2(builder);

            return builder.Build();
        }

        private static void ForceValor1(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceValor1, PerkType.ForceValor)
                .Name("Force Valor I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceValor, 30f)
                .RequirementFP(4)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    var statusEffectService = App.Resolve<IStatusEffectService>();
                    var combatPointService = App.Resolve<ICombatPointService>();
                    var enmityService = App.Resolve<IEnmityService>();

                    statusEffectService.Apply(activator, target, StatusEffectType.ForceValor1, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }

        private static void ForceValor2(IAbilityBuilder builder)
        {
            builder.Create(FeatType.ForceValor2, PerkType.ForceValor)
                .Name("Force Valor II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceValor, 30f)
                .RequirementFP(6)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willpowerBonus = GetAbilityModifier(AbilityType.Willpower, activator) * 30f;

                    var statusEffectService = App.Resolve<IStatusEffectService>();
                    var combatPointService = App.Resolve<ICombatPointService>();
                    var enmityService = App.Resolve<IEnmityService>();

                    statusEffectService.Apply(activator, target, StatusEffectType.ForceValor2, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }
    }
}
