using System.Collections.Generic;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.PerkDefinition
{
    public sealed class TwinPistolPerkDefinition : IPerkListDefinition
    {
        private readonly PerkBuilder _builder = new();

        public Dictionary<PerkType, PerkDetail> BuildPerks()
        {
            PairedVolley();
            AlternatingFire();
            MatchedRhythm();
            FocusedBurst();
            BalancedGrip();
            ControlledBurst();
            AkimboStance();
            ConvergingFire();
            CoolUnderFire();
            TwinSuns();
            LeadShot();
            Cadence();
            SteadyCadence();
            ClosingShot();
            ChangingAngles();
            TempoShot();
            CrossfireStance();
            TwinPistolFollowThrough();
            TwinPistolSecondWind();
            FinalSalvo();
            return _builder.Build();
        }

        private void PairedVolley()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.PairedVolley)
                .Name("Paired Volley")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley1)
                .DroidAISlots(1)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 4. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley2)
                .DroidAISlots(2)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 8. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 12)
                .RequirementMustHavePerk(PerkType.PairedVolley, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley3)
                .DroidAISlots(3)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 12. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 28)
                .RequirementMustHavePerk(PerkType.PairedVolley, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley4)
                .DroidAISlots(4)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 16. Works with any weapon.")
                .Price(5)
                .RequirementSkill(SkillType.TwinPistol, 40)
                .RequirementMustHavePerk(PerkType.PairedVolley, 3);
        }

        private void AlternatingFire()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.AlternatingFire)
                .Name("Alternating Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AlternatingFireTrait)
                .Description("While wielding two weapons, every three landed weapon auto-attacks restore 1 STM, at most once every 6 seconds. Misses do not reset the counter.")
                .IncreasesStat(StatType.PairedRangedOrdinaryHitRestoreCount, 3)
                .IncreasesStat(StatType.PairedRangedOrdinaryHitStaminaRestore, 1)
                .IncreasesStat(StatType.PairedRangedOrdinaryHitRestoreCooldownSeconds, 6)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 5);
        }

        private void MatchedRhythm()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.MatchedRhythm)
                .Name("Matched Rhythm")

                .AddPerkLevel()
                .GrantsFeat(FeatType.MatchedRhythmTrait)
                .Description("While wielding two weapons, grants +3% Haste.")
                .IncreasesStat(StatType.DualWieldAttackDelayReductionPercent, 3)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 8)

                .AddPerkLevel()
                .Description("While wielding two weapons, grants +6% Haste.")
                .IncreasesStat(StatType.DualWieldAttackDelayReductionPercent, 6)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 22)
                .RequirementMustHavePerk(PerkType.MatchedRhythm, 1)

                .AddPerkLevel()
                .Description("While wielding two weapons, grants +8% Haste.")
                .IncreasesStat(StatType.DualWieldAttackDelayReductionPercent, 8)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 45)
                .RequirementMustHavePerk(PerkType.MatchedRhythm, 2);
        }

        private void FocusedBurst()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.FocusedBurst)
                .Name("Focused Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst1)
                .DroidAISlots(1)
                .Description("Attack one enemy three times over 1.5 seconds. Each weapon deals a total of weapon DMG + 8, divided between the three attacks. Moving or being interrupted cancels the remaining attacks. Auto-attacks pause during the channel.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst2)
                .DroidAISlots(2)
                .Description("Attack one enemy three times over 1.5 seconds. Each weapon deals a total of weapon DMG + 16, divided between the three attacks. Moving or being interrupted cancels the remaining attacks. Auto-attacks pause during the channel.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 30)
                .RequirementMustHavePerk(PerkType.FocusedBurst, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst3)
                .DroidAISlots(3)
                .Description("Attack one enemy three times over 1.5 seconds. Each weapon deals a total of weapon DMG + 24, divided between the three attacks. Moving or being interrupted cancels the remaining attacks. Auto-attacks pause during the channel.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 38)
                .RequirementMustHavePerk(PerkType.FocusedBurst, 2);
        }

        private void BalancedGrip()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.BalancedGrip)
                .Name("Balanced Grip")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BalancedGripTrait)
                .Description("Grants +5 Accuracy.")
                .IncreasesStat(StatType.Accuracy, 5)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 15);
        }

        private void ControlledBurst()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.ControlledBurst)
                .Name("Controlled Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ControlledBurst1)
                .DroidAISlots(1)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals 75% of weapon DMG + 7. If any impact lands, restore 2 STM once. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ControlledBurst2)
                .DroidAISlots(2)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals 75% of weapon DMG + 13. If any impact lands, restore 3 STM once. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 32)
                .RequirementMustHavePerk(PerkType.ControlledBurst, 1);
        }

        private void AkimboStance()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.AkimboStance)
                .Name("Akimbo Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.AkimboStance)
                .DroidAISlots(1)
                .Description("With any weapon: +10% Haste. Landed weapon auto-attacks on the same enemy add +2% per stack, max five (+20% stance total), one stack per 2s. Target change or 6s without a weapon auto-attack hit clears stacks. All weapon auto-attacks deal 30% less damage.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 20);
        }

        private void ConvergingFire()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.ConvergingFire)
                .Name("Converging Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConvergingFireTrait)
                .Description("After three landed weapon auto-attacks on one enemy, gain +5% weapon auto-attack damage against it for 6 seconds. Further weapon auto-attack hits refresh it; changing targets clears it. Works with any weapon.")
                .IncreasesStat(StatType.PairedRangedRepeatedTargetRequiredHits, 3)
                .IncreasesStat(StatType.PairedRangedRepeatedTargetDamagePercent, 5)
                .IncreasesStat(StatType.PairedRangedRepeatedTargetDurationSeconds, 6)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 25);
        }

        private void CoolUnderFire()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.CoolUnderFire)
                .Name("Cool Under Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CoolUnderFireTrait)
                .Description("Taking direct hostile damage grants a 2 STM discount on your next combat ability that costs STM. One charge, lasts 10 seconds; triggers at most once every 10 seconds.")
                .IncreasesStat(StatType.PairedRangedDirectDamageNextAbilityStaminaReduction, 2)
                .IncreasesStat(StatType.PairedRangedDirectDamageCostWindowSeconds, 10)
                .IncreasesStat(StatType.PairedRangedDirectDamageCostCooldownSeconds, 10)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 35);
        }

        private void TwinSuns()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.TwinSuns)
                .Name("Twin Suns")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinSuns)
                .Description("Attack one enemy with each equipped attack weapon for weapon DMG + 20 per impact. Your next six weapon auto-attack attempts gain +25% damage for up to 12 seconds. Misses and deflections consume charges. Works with any weapon.")
                .Price(6)
                .RequirementSkill(SkillType.TwinPistol, 50)
                .RequirementQuest(TwinPistolCapstoneQuestDefinition.TwinSunsMasteryQuestId);
        }

        private void LeadShot()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.LeadShot)
                .Name("Lead Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.LeadShot1)
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 4. Grants 1 Cadence.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LeadShot2)
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 8. Grants 1 Cadence.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 12)
                .RequirementMustHavePerk(PerkType.LeadShot, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LeadShot3)
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .DroidAISlots(3)
                .Description("Deals weapon DMG + 12. Grants 1 Cadence.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 28)
                .RequirementMustHavePerk(PerkType.LeadShot, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.LeadShot4)
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .DroidAISlots(4)
                .Description("Deals weapon DMG + 16. Grants 1 Cadence.")
                .Price(5)
                .RequirementSkill(SkillType.TwinPistol, 40)
                .RequirementMustHavePerk(PerkType.LeadShot, 3);
        }

        private void Cadence()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.Cadence)
                .Name("Cadence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CadenceTrait)
                .Description("Landing a weapon ability within 6 seconds of a different one grants 1 Cadence, up to 3, for 12 seconds. Each Cadence grants +2 Accuracy to auto-attacks. Closing Shot, Final Salvo and other skills' weapon abilities spend all Cadence for +2 DMG per charge.")
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .IncreasesStat(StatType.CadenceChainWindowSeconds, 6)
                .IncreasesStat(StatType.CadenceAccuracyPerCharge, 2)
                .IncreasesStat(StatType.CadenceDamagePerCharge, 2)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 5);
        }

        private void SteadyCadence()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.SteadyCadence)
                .Name("Steady Cadence")

                .AddPerkLevel()
                .GrantsFeat(FeatType.SteadyCadenceTrait)
                .Description("Spending Cadence deals +1 DMG per charge.")
                .IncreasesStat(StatType.CadenceBonusDamagePerCharge, 1)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 8)
                .RequirementMustHavePerk(PerkType.Cadence)

                .AddPerkLevel()
                .Description("Spending Cadence deals +2 DMG per charge.")
                .IncreasesStat(StatType.CadenceBonusDamagePerCharge, 2)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 22)
                .RequirementMustHavePerk(PerkType.SteadyCadence, 1)

                .AddPerkLevel()
                .Description("Spending Cadence deals +3 DMG per charge.")
                .IncreasesStat(StatType.CadenceBonusDamagePerCharge, 3)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 45)
                .RequirementMustHavePerk(PerkType.SteadyCadence, 2);
        }

        private void ClosingShot()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.ClosingShot)
                .Name("Closing Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ClosingShot1)
                .DroidAISlots(1)
                .Description("Deals weapon DMG + 6. Spends all Cadence for +2 DMG per charge.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ClosingShot2)
                .DroidAISlots(2)
                .Description("Deals weapon DMG + 10. Spends all Cadence for +2 DMG per charge.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 30)
                .RequirementMustHavePerk(PerkType.ClosingShot, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ClosingShot3)
                .DroidAISlots(3)
                .Description("Deals weapon DMG + 14. Spends all Cadence for +2 DMG per charge.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 38)
                .RequirementMustHavePerk(PerkType.ClosingShot, 2);
        }

        private void ChangingAngles()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.ChangingAngles)
                .Name("Changing Angles")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ChangingAnglesTrait)
                .Description("Gaining Cadence gives your next weapon ability +5 Accuracy for 8 seconds. Triggers once every 10 seconds.")
                .IncreasesStat(StatType.CadenceNextAbilityAccuracy, 5)
                .IncreasesStat(StatType.CadenceAccuracyDurationSeconds, 8)
                .IncreasesStat(StatType.CadenceAccuracyCooldownSeconds, 10)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 15);
        }

        private void TempoShot()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TempoShot)
                .Name("Tempo Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempoShot1)
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .DroidAISlots(1)
                .Description("Deals 60% weapon DMG + 6. Grants 2 Cadence.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.TempoShot2)
                .IncreasesStat(StatType.CadenceMaximum, 3)
                .IncreasesStat(StatType.CadenceDurationSeconds, 12)
                .DroidAISlots(2)
                .Description("Deals 60% weapon DMG + 12. Grants 2 Cadence.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 32)
                .RequirementMustHavePerk(PerkType.TempoShot, 1);
        }

        private void CrossfireStance()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.CrossfireStance)
                .Name("Crossfire Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrossfireStance)
                .DroidAISlots(1)
                .Description("While active, grants +10% Haste and +10% Movement Speed and Cadence lasts 18 seconds, but auto-attacks deal 20% less damage.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 20);
        }

        private void TwinPistolFollowThrough()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolFollowThrough)
                .Name("Follow Through")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolFollowThroughTrait)
                .Description("Spending Cadence on a hit restores 1 STM per charge, up to 3 STM. Triggers once every 12 seconds.")
                .IncreasesStat(StatType.CadenceStaminaRestorePerCharge, 1)
                .IncreasesStat(StatType.CadenceStaminaRestoreMaximum, 3)
                .IncreasesStat(StatType.CadenceRestoreCooldownSeconds, 12)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 25);
        }

        private void TwinPistolSecondWind()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolSecondWind)
                .Name("Second Wind")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolSecondWindTrait)
                .Description("After you evade an attack, gain 1 Cadence. Triggers once every 10 seconds.")
                .IncreasesStat(StatType.EvasionCadenceCooldownSeconds, 10)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 35)
                .RequirementMustHavePerk(PerkType.Cadence);
        }

        private void FinalSalvo()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.FinalSalvo)
                .Name("Final Salvo")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FinalSalvo)
                .Description("Deals weapon DMG + 20. Spends all Cadence for +4 DMG per charge.")
                .Price(6)
                .RequirementSkill(SkillType.TwinPistol, 50)
                .RequirementQuest(TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId);
        }
    }
}
