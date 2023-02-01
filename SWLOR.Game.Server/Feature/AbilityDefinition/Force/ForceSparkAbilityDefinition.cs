using System.Collections.Generic;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.VisualEffect;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.Force
{
    public class ForceSparkAbilityDefinition : IAbilityListDefinition
    {
        private readonly AbilityBuilder _builder = new();
        private const string Tier1Tag = "ABILITY_FORCE_SPARK_1";
        private const string Tier2Tag = "ABILITY_FORCE_SPARK_2";
        private const string Tier3Tag = "ABILITY_FORCE_SPARK_3";

        public Dictionary<FeatType, AbilityDetail> BuildAbilities()
        {
            ForceSpark1();
            ForceSpark2();
            ForceSpark3();

            return _builder.Build();
        }
        private void Impact(uint activator, uint target, int dmg, int evaDecrease, int tier, string effectTag, int dc)
        {
            var attackerStat = GetAbilityScore(activator, AbilityType.Willpower);
            var defenderStat = GetAbilityScore(target, AbilityType.Willpower);
            var attack = Stat.GetAttack(activator, AbilityType.Willpower, SkillType.Force);
            var defense = Stat.GetDefense(target, CombatDamageType.Force, AbilityType.Willpower);
            var damage = Combat.CalculateDamage(attack, dmg, attackerStat, defense, defenderStat, 0);


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

                dc = Combat.CalculateSavingThrowDC(activator, SavingThrow.Fortitude, dc, AbilityType.Willpower);
                var checkResult = FortitudeSave(target, dc, SavingThrowType.None, activator);

                if (checkResult == SavingThrowResultType.Failed)
                {
                    var breach = TagEffect(EffectACDecrease(evaDecrease), effectTag);
                    ApplyEffectToObject(DurationType.Temporary, breach, target, 60f);
                    Messaging.SendMessageNearbyToPlayers(target, $"{GetName(target)} receives the effect of evasion down.");
                }
            }

            ApplyEffectToObject(DurationType.Instant, EffectDamage(damage), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Starburst_Red), target);
            ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Beam_Lightning, false, 2f), target);

            Enmity.ModifyEnmity(activator, target, 300 + damage);
            CombatPoint.AddCombatPoint(activator, target, SkillType.Force, 3);
        }

        private void ForceSpark1()
        {
            _builder.Create(FeatType.ForceSpark1, PerkType.ForceSpark)
                .Name("Force Spark I")
                .Level(1)
                .HasRecastDelay(RecastGroup.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
                    var willDMG = 10 + (willBonus * 2);
                    if (Stat.GetCurrentFP(activator) == 0)
                    {
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(7), activator);
                    }
                    else { Stat.ReduceFP(activator, 1); }
                    Impact(activator, target, willDMG, 2, 1, Tier1Tag, 8);
                });
        }

        private void ForceSpark2()
        {
            _builder.Create(FeatType.ForceSpark2, PerkType.ForceSpark)
                .Name("Force Spark II")
                .Level(2)
                .HasRecastDelay(RecastGroup.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
                    var willDMG = 30 + (willBonus * 4);
                    if (Stat.GetCurrentFP(activator) < 3)
                    {
                        var darkBargain = 7 * (3 - Stat.GetCurrentFP(activator));
                        Stat.ReduceFP(activator, Stat.GetCurrentFP(activator));
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
                    }
                    else { Stat.ReduceFP(activator, 3); }
                    Impact(activator, target, willDMG, 4, 2, Tier2Tag, 12);
                });
        }

        private void ForceSpark3()
        {
            _builder.Create(FeatType.ForceSpark3, PerkType.ForceSpark)
                .Name("Force Spark III")
                .Level(3)
                .HasRecastDelay(RecastGroup.ForceSpark, 6f)
                .HasActivationDelay(2f)
                .IsCastedAbility()
                .HasMaxRange(10f)
                .IsHostileAbility()
                .UsesAnimation(Animation.LoopingConjure1)
                .DisplaysVisualEffectWhenActivating()
                .HasImpactAction((activator, target, level, location) =>
                {
                    var willBonus = GetAbilityModifier(AbilityType.Willpower, activator);
                    var willDMG = 50 + (willBonus * 6);
                    if (Stat.GetCurrentFP(activator) < 5)
                    {
                        var darkBargain = 7 * (5 - Stat.GetCurrentFP(activator));
                        Stat.ReduceFP(activator, Stat.GetCurrentFP(activator));
                        ApplyEffectToObject(DurationType.Instant, EffectDamage(darkBargain), activator);
                    } 
                    else { Stat.ReduceFP(activator, 5); }
                    Impact(activator, target, willDMG, 6, 3, Tier3Tag, 14);
                });
        }
    }
}
