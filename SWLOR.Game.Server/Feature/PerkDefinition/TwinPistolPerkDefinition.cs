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
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 10. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 12)
                .RequirementMustHavePerk(PerkType.PairedVolley, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley3)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 18. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 28)
                .RequirementMustHavePerk(PerkType.PairedVolley, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PairedVolley4)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 26. Works with any weapon.")
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
                .Description("Every three landed ordinary weapon attacks restore 1 STM, at most once every 6 seconds. Misses do not reset the counter. Works with any weapon.")
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
                .Description("Grants +3% Haste with any weapon. Cannot be purchased with Paired Handling.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, 3)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 8)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.PairedHandling)
                .Description("Grants +6% Haste with any weapon. Cannot be purchased with Paired Handling.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, 6)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 22)
                .RequirementMustHavePerk(PerkType.MatchedRhythm, 1)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.PairedHandling)
                .Description("Grants +8% Haste with any weapon. Cannot be purchased with Paired Handling.")
                .IncreasesStat(StatType.AttackDelayReductionPercent, 8)
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
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 10. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst2)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 22. Works with any weapon.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 30)
                .RequirementMustHavePerk(PerkType.FocusedBurst, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.FocusedBurst3)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals weapon DMG + 34. Works with any weapon.")
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
                .Description("Grants +5 ACC to an eligible off-hand attack weapon, including a companion pistol. Shields receive no bonus.")
                .IncreasesStat(StatType.PairedRangedCompanionAccuracy, 5)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 15);
        }

        private void ControlledBurst()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.ControlledBurst)
                .Name("Controlled Burst")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ControlledBurst1)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals floor(0.75 x weapon DMG) + 7. If any impact lands, restore 2 STM once. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.ControlledBurst2)
                .Description("Attack one enemy with each equipped attack weapon. Each impact deals floor(0.75 x weapon DMG) + 13. If any impact lands, restore 3 STM once. Works with any weapon.")
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
                .Description("With any weapon: +10% Haste. Landed ordinary attacks on the same enemy add +2% per stack, max five (+20% stance total), one stack per 2s. Target change or 6s without a landed ordinary hit clears stacks. ALL ordinary weapon attacks deal 15% less damage.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 20);
        }

        private void ConvergingFire()
        {
            _builder.Create(PerkCategoryType.TwinPistolBarrage, PerkType.ConvergingFire)
                .Name("Converging Fire")

                .AddPerkLevel()
                .GrantsFeat(FeatType.ConvergingFireTrait)
                .Description("After three landed ordinary weapon attacks on one enemy, gain +5% ordinary weapon damage against it for 6 seconds. Further landed ordinary hits refresh it; changing targets clears it. Works with any weapon.")
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
                .Description("Taking direct hostile damage makes your next Twin Pistol ability cost 2 less STM. One charge, lasts 10 seconds; triggers at most once every 10 seconds.")
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
                .Description("Attack one enemy with each equipped attack weapon for weapon DMG + 20 per impact. Your next six ordinary weapon attacks gain +25% damage for up to 12 seconds. Misses and deflections consume charges. Works with any weapon.")
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
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 10 per impact. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 12)
                .RequirementMustHavePerk(PerkType.BlasterSlide, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlasterSlide3)
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 18 per impact. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 28)
                .RequirementMustHavePerk(PerkType.BlasterSlide, 2)

                .AddPerkLevel()
                .GrantsFeat(FeatType.BlasterSlide4)
                .Description("Retreat 4m directly away from the selected enemy, then attack with each equipped attack weapon for weapon DMG + 26 per impact. Works with any weapon.")
                .Price(5)
                .RequirementSkill(SkillType.TwinPistol, 40)
                .RequirementMustHavePerk(PerkType.BlasterSlide, 3);
        }

        private void TwinPistolMomentum()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolMomentum)
                .Name("Momentum")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolMomentumTrait)
                .Description("Slide or Rush moving you at least 2m and landing any hostile hit grants 1 Momentum, max 3. Lasts 12 seconds. Each charge gives +2 ACC to ordinary attacks with any weapon. Grapple Shot and Final Salvo spend all charges.")
                .IncreasesStat(StatType.PairedRangedMomentumMaximum, 3)
                .IncreasesStat(StatType.PairedRangedMomentumDurationSeconds, 12)
                .IncreasesStat(StatType.PairedRangedMomentumAccuracyPerCharge, 2)
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
                .Description("Ordinary weapon attacks deal +10% damage with any weapon. Cannot be purchased with Matched Rhythm.")
                .IncreasesStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment, 10)
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 8)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.MatchedRhythm)
                .Description("Ordinary weapon attacks deal +20% damage with any weapon. Cannot be purchased with Matched Rhythm.")
                .IncreasesStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment, 20)
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 22)
                .RequirementMustHavePerk(PerkType.PairedHandling, 1)

                .AddPerkLevel()
                .RequirementCannotHavePerk(PerkType.MatchedRhythm)
                .Description("Ordinary weapon attacks deal +25% damage with any weapon. Cannot be purchased with Matched Rhythm.")
                .IncreasesStat(StatType.PairedRangedOrdinaryDamagePercentAdjustment, 25)
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
                .Description("Close up to 6m toward an enemy, then attack with each equipped attack weapon for weapon DMG + 6 per impact. Spend all Momentum for +4 power per charge on each impact. Works with any weapon.")
                .Price(2)
                .RequirementSkill(SkillType.TwinPistol, 10)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GrappleShot2)
                .Description("Close up to 6m toward an enemy, then attack with each equipped attack weapon for weapon DMG + 13 per impact. Spend all Momentum for +5 power per charge on each impact. Works with any weapon.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 30)
                .RequirementMustHavePerk(PerkType.GrappleShot, 1)

                .AddPerkLevel()
                .GrantsFeat(FeatType.GrappleShot3)
                .Description("Close up to 6m toward an enemy, then attack with each equipped attack weapon for weapon DMG + 20 per impact. Spend all Momentum for +6 power per charge on each impact. Works with any weapon.")
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
                .Description("A landed Crossfire attack after repositioning at least 2m gives your next Crossfire-line ability +5 ACC. One charge, lasts 8 seconds; triggers at most once every 10 seconds.")
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
                .Description("Dash to a freely selected ground point 2-6m away. Attack up to three enemies in the 4m-wide travel line with each equipped attack weapon for floor(0.60 x weapon DMG) + 6 per impact. Works with any weapon.")
                .Price(3)
                .RequirementSkill(SkillType.TwinPistol, 18)

                .AddPerkLevel()
                .GrantsFeat(FeatType.PistolRush2)
                .Description("Dash to a freely selected ground point 2-6m away. Attack up to three enemies in the 4m-wide travel line with each equipped attack weapon for floor(0.60 x weapon DMG) + 12 per impact. Works with any weapon.")
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
                .Description("With any weapon: +10% Haste, +10% Movement Speed and 18-second Momentum. ALL ordinary weapon attacks deal 10% less damage. Activated ability damage is unchanged.")
                .Price(4)
                .RequirementSkill(SkillType.TwinPistol, 20);
        }

        private void TwinPistolFollowThrough()
        {
            _builder.Create(PerkCategoryType.TwinPistolCrossfire, PerkType.TwinPistolFollowThrough)
                .Name("Follow Through")

                .AddPerkLevel()
                .GrantsFeat(FeatType.TwinPistolFollowThroughTrait)
                .Description("A Momentum-spending ability landing any hit restores 1 STM per charge spent, up to 3 STM. At most once per activation and once every 12 seconds.")
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
                .Description("A direct hostile attack missing its accuracy-versus-evasion roll grants 1 Momentum, at most once every 10 seconds.")
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
                .Description("Without moving, attack one enemy with each equipped attack weapon for weapon DMG + 20 per impact. Spend all Momentum for +6 power per charge on each impact (max +18). Works with any weapon.")
                .Price(6)
                .RequirementSkill(SkillType.TwinPistol, 50)
                .RequirementQuest(TwinPistolCapstoneQuestDefinition.FinalSalvoMasteryQuestId);
        }
    }
}

