using SWLOR.Game.Server.Service.AbilityService;
using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Espionage
{
    public class TacticalEscapeAbilityDefinition: IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            TacticalEscape1();
            TacticalEscape2();

            return _builder.Build();
        }

        private string Validate(uint activator)
        {
            if (!Enmity.HasEnmity(activator))
                return "You are not being targeted.";

            return string.Empty;
        }

        private void ApplyEffects(uint activator, int statIncrease)
        {
            var perception = GetAbilityScore(activator, AbilityType.Perception) - 10;
            if (perception <= 0)
                perception = 1;

            var percentReduction = statIncrease * perception;
            Enmity.ModifyEnmityOnAllByPercent(activator, -percentReduction);
            ApplyEffectToObject(DurationType.Temporary, EffectVisualEffect(VisualEffect.Vfx_Dur_Aura_Magenta), activator, 3f);
        }

        private void TacticalEscape1()
        {
            _builder.Create(FeatType.TacticalEscape1, PerkType.TacticalEscape)
                .Name("Tactical Escape I")
                .Level(1)
                .HasRecastDelay(RecastGroup.TacticalEscape, 60f * 3f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .RequirementStamina(15)
                .HasCustomValidation((activator, _, _, _) => Validate(activator))
                .HasImpactAction((activator, _, _, _) =>
                {
                    ApplyEffects(activator, 5);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Espionage, 6);
                });
        }

        private void TacticalEscape2()
        {
            _builder.Create(FeatType.TacticalEscape2, PerkType.TacticalEscape)
                .Name("Tactical Escape II")
                .Level(2)
                .HasRecastDelay(RecastGroup.TacticalEscape, 60f * 3f)
                .UsesAnimation(Animation.LoopingGetMid)
                .HasActivationDelay(6f)
                .IsCastedAbility()
                .RequirementStamina(30)
                .HasCustomValidation((activator, _, _, _) => Validate(activator))
                .HasImpactAction((activator, _, _, _) =>
                {
                    ApplyEffects(activator, 10);
                    CombatPoint.AddCombatPointToAllTagged(activator, SkillType.Espionage, 8);
                });
        }
    }
}
