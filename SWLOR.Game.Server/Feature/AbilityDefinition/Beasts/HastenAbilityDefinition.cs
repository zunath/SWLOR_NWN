using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Beasts
{
    public class HastenAbilityDefinition: IAbilityListDefinition
    {
        public const string HastenEffectTag = "BEAST_HASTEN";

        private readonly AbilityBuilder _builder = new();
        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Hasten1();
            Hasten2();
            Hasten3();

            return _builder.Build();
        }

        private void Impact(uint activator, int numAttacks, bool applyToBeastmaster)
        {
            var beastmaster = GetMaster(activator);
            var beastmasterStat = GetAbilityModifier(AbilityType.Agility, beastmaster);
            var beastStat = GetAbilityModifier(AbilityType.Agility, activator);
            var bonusDuration = 3f * (beastmasterStat + beastStat);

            var effect = EffectModifyAttacks(numAttacks);
            effect = TagEffect(effect, HastenEffectTag);

            ApplyEffectToObject(DurationType.Temporary, effect, activator, 30f + bonusDuration);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Haste), activator);

            if (applyToBeastmaster)
            {
                effect = EffectModifyAttacks(1);
                effect = TagEffect(effect, HastenEffectTag);

                ApplyEffectToObject(DurationType.Temporary, effect, beastmaster, 30f + bonusDuration);
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Haste), beastmaster);
            }

            Enmity.ModifyEnmityOnAll(activator, 300 * numAttacks);
        }

        private void Hasten1()
        {
            _builder.Create(FeatType.Hasten1, PerkType.Hasten)
                .Name("Hasten I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Hasten, 120f)
                .HasActivationDelay(1f)
                .RequirementStamina(5)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, 1, false);
                });
        }

        private void Hasten2()
        {
            _builder.Create(FeatType.Hasten2, PerkType.Hasten)
                .Name("Hasten II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Hasten, 120f)
                .HasActivationDelay(1f)
                .RequirementStamina(6)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, 2, false);
                });
        }

        private void Hasten3()
        {
            _builder.Create(FeatType.Hasten3, PerkType.Hasten)
                .Name("Hasten III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Hasten, 120f)
                .HasActivationDelay(1f)
                .RequirementStamina(7)
                .IsCastedAbility()
                .UnaffectedByHeavyArmor()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, 2, true);
                });
        }
    }
}
