using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.MartialArts
{
    public class ChiAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Chi1();
            Chi2();
            Chi3();

            return _builder.Build();
        }

        private void ImpactAction(uint activator, int baseRecovery)
        {
            var bonusRecovery = GetAbilityModifier(AbilityType.Willpower, activator) * 8;
            var recovery = baseRecovery + bonusRecovery;

            ApplyEffectToObject(DurationType.Instant, EffectHeal(recovery), activator);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Healing_G), activator);

            Enmity.ModifyEnmityOnAll(activator, 300 + recovery + 10);
            CombatPoint.AddCombatPointToAllTagged(activator, SkillType.MartialArts, 3);
        }

        private void Chi1()
        {
            _builder.Create(FeatType.Chi1, PerkType.Chi)
                .Name("Chi I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Chi, 180f)
                .HasActivationDelay(1.0f)
                .RequirementStamina(4)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 45);
                });
        }

        private void Chi2()
        {
            _builder.Create(FeatType.Chi2, PerkType.Chi)
                .Name("Chi II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Chi, 180f)
                .HasActivationDelay(2.0f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 115);
                });
        }

        private void Chi3()
        {
            _builder.Create(FeatType.Chi3, PerkType.Chi)
                .Name("Chi III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Chi, 180f)
                .HasActivationDelay(3.0f)
                .RequirementStamina(10)
                .IsCastedAbility()
                .HasImpactAction((activator, target, level, location) =>
                {
                    ImpactAction(activator, 170);
                });
        }
    }
}
