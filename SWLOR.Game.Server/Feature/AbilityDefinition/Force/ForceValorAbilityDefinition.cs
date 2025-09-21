using System.Collections.Generic;
using SWLOR.Game.Server.Service;


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
        private readonly AbilityBuilder _builder = new();
        private readonly ICombatPointService _combatPointService;
        private readonly IEnmityService _enmityService;
        private readonly IStatusEffectService _statusEffectService;

        public ForceValorAbilityDefinition(ICombatPointService combatPointService, IEnmityService enmityService, IStatusEffectService statusEffectService)
        {
            _combatPointService = combatPointService;
            _enmityService = enmityService;
            _statusEffectService = statusEffectService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceValor1();
            ForceValor2();

            return _builder.Build();
        }

        private void ForceValor1()
        {
            _builder.Create(FeatType.ForceValor1, PerkType.ForceValor)
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

                    _statusEffectService.Apply(activator, target, StatusEffectType.ForceValor1, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    _enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }

        private void ForceValor2()
        {
            _builder.Create(FeatType.ForceValor2, PerkType.ForceValor)
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

                    _statusEffectService.Apply(activator, target, StatusEffectType.ForceValor2, 60f * 15f + willpowerBonus);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), target);

                    _combatPointService.AddCombatPointToAllTagged(activator, SkillType.Force, 3);
                    _enmityService.ModifyEnmityOnAll(activator, 250 * level);
                });
        }
    }
}
