using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Enums;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class DisturbanceAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private readonly ICombatService _combatService;
        private readonly IStatService _statService;
        private readonly CombatPoint _combatPoint;
        private readonly IEnmityService _enmityService;
        private const string Tier1Tag = "EFFECT_DISTURBANCE_1";
        private const string Tier2Tag = "EFFECT_DISTURBANCE_2";
        private const string Tier3Tag = "EFFECT_DISTURBANCE_3";

        public DisturbanceAbilityDefinition(ICombatService combatService, IStatService statService, CombatPoint combatPoint, IEnmityService enmityService)
        {
            _combatService = combatService;
            _statService = statService;
            _combatPoint = combatPoint;
            _enmityService = enmityService;
        }

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            Disturbance1();
            Disturbance2();
            Disturbance3();

            return _builder.Build();
        }

        private void Impact(uint activator, uint target, int dmg, int accDecrease, int tier, string effectTag, int dc)
        {
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = _statService.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = _statService.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            dmg += (attackerStat * ((tier -1) / 2)) + attackerStat;
            var damage = _combatService.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);

            if (HasMorePowerfulEffect(target, tier,
                    new(Tier1Tag, 1),
                    new(Tier2Tag, 2),
                    new(Tier3Tag, 3)))
            {
                SendMessageToPC(activator, "Your target is already afflicted by a more powerful effect.");
            }
            else
            {
                RemoveEffectByTag(target, Tier1Tag, Tier2Tag, Tier3Tag);

                dc = _combatService.CalculateSavingThrowDC(activator, SavingThrow.Will, dc);
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

            _enmityService.ModifyEnmityOnAll(activator, 150 + damage);
            _combatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void Disturbance1()
        {
            _builder.Create(FeatType.Disturbance1, PerkType.Disturbance)
                .Name("Disturbance I")
                .Level(1)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 0, 2, 1, Tier1Tag, 8);
                });
        }

        private void Disturbance2()
        {
            _builder.Create(FeatType.Disturbance2, PerkType.Disturbance)
                .Name("Disturbance II")
                .Level(2)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(1)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
                    var willDMG = 30 + (willBonus * 4);
                    Impact(activator, target, 15, 4, 2, Tier2Tag, 12);
                });
        }

        private void Disturbance3()
        {
            _builder.Create(FeatType.Disturbance3, PerkType.Disturbance)
                .Name("Disturbance III")
                .Level(3)
                .HasRecastDelay(RecastGroup.Disturbance, 6f)
                .HasActivationDelay(2f)
                .RequirementFP(2)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .BreaksStealth()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    Impact(activator, target, 30, 6, 3, Tier3Tag, 14);
                });
        }
    }
}