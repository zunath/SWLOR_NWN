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
            BlasterSlide();
            TwinPistolMomentum();
            PairedHandling();
            GrappleShot();
            ChangingAngles();
            PistolRush();
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
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 4. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley2)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 8. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 12)
                .RequirementMustHavePerk(PerkType.PairedVolley, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley3)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 12. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 28)
                .RequirementMustHavePerk(PerkType.PairedVolley, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley4)
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
                .RequirementCannotHavePerk(PerkType.PairedHandling)
                .GrantsFeat(FeatType.MatchedRhythmTrait)
                .Description("While wielding two weapons, grants +3% Haste. Cannot be purchased with Paired Handling.")
                .IncreasesStat(StatType.DualWieldAttackDelayReductionPercent, 3)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 8)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.PairedHandling)
                .Description("While wielding two weapons, grants +6% Haste. Cannot be purchased with Paired Handling.")
                .IncreasesStat(StatType.DualWieldAttackDelayReductionPercent, 6)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 22)
                .RequirementMustHavePerk(PerkType.MatchedRhythm, 1)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.PairedHandling)
                .Description("While wielding two weapons, grants +8% Haste. Cannot be purchased with Paired Handling.")
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
                .Description("Attack one enemy three times over 1.5 seconds. Each weapon deals a total of weapon DMG + 8, divided between the three attacks. Moving or being interrupted cancels the remaining attacks. Auto-attacks pause during the channel.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst2)
                .Description("Attack one enemy three times over 1.5 seconds. Each weapon deals a total of weapon DMG + 16, divided between the three attacks. Moving or being interrupted cancels the remaining attacks. Auto-attacks pause during the channel.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 30)
                .RequirementMustHavePerk(PerkType.FocusedBurst, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst3)
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
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals 75% of weapon DMG + 7. If any impact lands, restore 2 STM once. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ControlledBurst2)
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

        private void BlasterSlide()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.BlasterSlide)
                .Name("Blaster Slide")

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlasterSlide1)
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 4 per impact. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlasterSlide2)
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 8 per impact. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 12)
                .RequirementMustHavePerk(PerkType.BlasterSlide, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlasterSlide3)
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 12 per impact. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 28)
                .RequirementMustHavePerk(PerkType.BlasterSlide, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlasterSlide4)
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 16 per impact. Works with any weapon.")
                .Price(5)
                .RequirementSkill(SkillType.TwinPistol, 40)
                .RequirementMustHavePerk(PerkType.BlasterSlide, 3);
        }

        private void TwinPistolMomentum()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolMomentum)
                .Name("Flow")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolMomentumTrait)
                .Description("After moving at least 2m, landing a hostile weapon attack grants 1 Flow, max 3. Lasts 12 seconds. Each charge gives +2 Accuracy to weapon auto-attacks. Your next hostile weapon ability that costs STM spends all Flow for +2 DMG per charge, applied once per weapon across the whole ability.")
                .IncreasesStat(StatType.PairedRangedMomentumMaximum, 3)
                .IncreasesStat(StatType.PairedRangedMomentumDurationSeconds, 12)
                .IncreasesStat(StatType.PairedRangedMomentumAccuracyPerCharge, 2)
                .IncreasesStat(StatType.MovementHitWeaponAbilityDamagePerCharge, 2)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 5);
        }

        private void PairedHandling()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.PairedHandling)
                .Name("Paired Handling")

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.MatchedRhythm)
                .GrantsFeat(FeatType.PairedHandlingTrait)
                .Description("While wielding two weapons, weapon auto-attacks deal +5% damage. Cannot be purchased with Matched Rhythm.")
                .IncreasesStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment, 5)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 8)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.MatchedRhythm)
                .Description("While wielding two weapons, weapon auto-attacks deal +8% damage. Cannot be purchased with Matched Rhythm.")
                .IncreasesStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment, 8)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 22)
                .RequirementMustHavePerk(PerkType.PairedHandling, 1)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.MatchedRhythm)
                .Description("While wielding two weapons, weapon auto-attacks deal +10% damage. Cannot be purchased with Matched Rhythm.")
                .IncreasesStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment, 10)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 45)
                .RequirementMustHavePerk(PerkType.PairedHandling, 2);
        }

        private void GrappleShot()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.GrappleShot)
                .Name("Grapple Shot")

                .AddPerkLevel()
                .GrantsFeat(FeatType.GrappleShot1)
                .Description("Close up to 6m toward an enemy, then attack with each equipped attack weapon for weapon DMG + 6 per impact. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GrappleShot2)
                .Description("Close up to 6m toward an enemy, then attack with each equipped attack weapon for weapon DMG + 10 per impact. Works with any weapon.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 30)
                .RequirementMustHavePerk(PerkType.GrappleShot, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GrappleShot3)
                .Description("Close up to 6m toward an enemy, then attack with each equipped attack weapon for weapon DMG + 14 per impact. Works with any weapon.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 38)
                .RequirementMustHavePerk(PerkType.GrappleShot, 2);
        }

        private void ChangingAngles()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.ChangingAngles)
                .Name("Changing Angles")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ChangingAnglesTrait)
                .Description("After moving at least 2m and landing a hostile weapon attack, your next hostile weapon ability gains +5 Accuracy. One charge, lasts 8 seconds; triggers at most once every 10 seconds.")
                .IncreasesStat(StatType.PairedRangedMovementNextAbilityAccuracy, 5)
                .IncreasesStat(StatType.PairedRangedMovementAccuracyDurationSeconds, 8)
                .IncreasesStat(StatType.PairedRangedMovementAccuracyCooldownSeconds, 10)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 15);
        }

        private void PistolRush()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.PistolRush)
                .Name("Pistol Rush")

                .AddPerkLevel()
                .GrantsFeat(FeatType.PistolRush1)
                .Description("Dash to a freely selected ground point 2-6m away. Attack up to three enemies in the 4m-wide travel line with each equipped attack weapon for 60% of weapon DMG + 6 per impact. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PistolRush2)
                .Description("Dash to a freely selected ground point 2-6m away. Attack up to three enemies in the 4m-wide travel line with each equipped attack weapon for 60% of weapon DMG + 12 per impact. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 32)
                .RequirementMustHavePerk(PerkType.PistolRush, 1);
        }

        private void CrossfireStance()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.CrossfireStance)
                .Name("Crossfire Stance")

                .AddPerkLevel()
                .GrantsFeat(FeatType.CrossfireStance)
                .Description("With any weapon: +10% Haste, +10% Movement Speed and 18-second Flow. All weapon auto-attacks deal 20% less damage. Activated ability damage is unchanged.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 20);
        }

        private void TwinPistolFollowThrough()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolFollowThrough)
                .Name("Follow Through")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolFollowThroughTrait)
                .Description("A Flow-spending ability landing any hit restores 1 STM per charge spent, up to 3 STM. Cannot reduce the ability's net STM cost below 1. At most once per activation and once every 12 seconds.")
                .IncreasesStat(StatType.PairedRangedMomentumStaminaRestorePerCharge, 1)
                .IncreasesStat(StatType.PairedRangedMomentumStaminaRestoreMaximum, 3)
                .IncreasesStat(StatType.PairedRangedMomentumRestoreCooldownSeconds, 12)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 25);
        }

        private void TwinPistolSecondWind()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolSecondWind)
                .Name("Second Wind")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolSecondWindTrait)
                .Description("A direct hostile attack missing its accuracy-versus-evasion roll grants 1 Flow, at most once every 10 seconds.")
                .IncreasesStat(StatType.PairedRangedEvasionMomentumCooldownSeconds, 10)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 35)
                .RequirementMustHavePerk(PerkType.TwinPistolMomentum);
        }

        private void FinalSalvo()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.FinalSalvo)
                .Name("Final Salvo")

                .AddPerkLevel()
                .GrantsFeat(FeatType.FinalSalvo)
                .Description("Without moving, attack one enemy with each equipped attack weapon for weapon DMG + 20 per impact. Spend all Flow for +4 DMG per charge on each impact (max +12). Works with any weapon.")
                .Price(6)
                .RequirementSkill(SkillType.TwinPistol, 50)
                .RequirementQuest(TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId);
        }
    }
}

