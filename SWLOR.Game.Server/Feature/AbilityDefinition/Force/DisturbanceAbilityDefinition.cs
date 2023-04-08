using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class DisturbanceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private const string Tier1Tag = "EFFECT_DISTURBANCE_1";
        private const string Tier2Tag = "EFFECT_DISTURBANCE_2";
        private const string Tier3Tag = "EFFECT_DISTURBANCE_3";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Disturbance1();
            Disturbance2();
            Disturbance3();

            return _builder.Build();
        }

        private static void ImpactAction(uint activator, uint target, int level, Location targetLocation)
        {
            var willBonus = GetAbilityScore(activator, AbilityType.Willpower);
            var dmg = 0;
            string effectTag = "";
            int dc = 0;
            int accDecrease = 0;

            switch (level)
            {
                case 1:
                    dmg = willBonus;
                    effectTag = "Tier1Tag";
                    dc = 8;
                    accDecrease = 2;
                    break;
                case 2:
                    dmg = 10 + (willBonus * 3 / 2);
                    effectTag = "Tier2Tag";
                    dc = 12;
                    accDecrease = 4;
                    break;
                case 3:
                    dmg = 20 + (willBonus * 2);
                    effectTag = "Tier3Tag";
                    dc = 14;
                    accDecrease = 6;
                    break;
            }

            dmg += Combat.GetAbilityDamageBonus(activator, SkillType.Force);

            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(
                attack,
                dmg,
                attackerStat,
                defense,
                defenderStat,
                0);

            if (HasMorePowerfulEffect(target, level,
                    new(Tier1Tag, 1),
                    new(Tier2Tag, 2),
                    new(Tier3Tag, 3)))
            {
                SendMessageToPC(activator, "Your target is already afflicted by a more powerful effect.");
            }
            else
            {
                RemoveEffectByTag(target, Tier1Tag, Tier2Tag, Tier3Tag);

                dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

                if (checkResult == SavingThrowResultType.Failed)
                {
                    var accuracyDown = TagEffect(EffectAccuracyDecrease(accDecrease), effectTag);
                    ApplyEffectToObject(DurationType.Temporary, accuracyDown, target, 60f);
                    Messaging.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of accuracy down.");
                }
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Green), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Holy), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Pulse_Wind), target);

            Enmity.ModifyEnmity(activator, target, level * 150 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void Disturbance1()
        {
            _builder.Create(FeatType.Disturbance1, PerkType.Disturbance)
                .Name("Disturbance I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(1)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void Disturbance2()
        {
            _builder.Create(FeatType.Disturbance2, PerkType.Disturbance)
                .Name("Disturbance II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(2)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }

        private void Disturbance3()
        {
            _builder.Create(FeatType.Disturbance3, PerkType.Disturbance)
                .Name("Disturbance III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(3)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction(ImpactAction);
        }
    }
}