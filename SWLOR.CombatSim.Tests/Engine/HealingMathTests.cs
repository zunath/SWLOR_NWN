using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class HealingMathTests
    {
        private const float PreTuningAffinityCap = 1.5f;
        private const float CurrentAffinityCap = 1.25f;
        private const int LightAffinity = 10;
        private const int MaxScalingWillpower = 26;
        private const int BaselineWillpower = 10;
        private const int TargetMaxHp = 1000;

        private static AbilityEffect Effect(FeatType feat)
        {
            EffectCatalog.TryGet(feat, out var effect).Should().BeTrue($"{feat} is a modeled ability");
            return effect;
        }

        [TestCase(0, 0f)]
        [TestCase(10, 0f)]
        [TestCase(18, 0.5f)]
        [TestCase(26, 1f)]
        [TestCase(27, 1f)]
        [TestCase(60, 1f)]
        public void StatProgress_ClampsBetweenBaselineAndCap(int stat, float expected)
        {
            HealingMath.StatProgress(stat).Should().BeApproximately(expected, 1e-6f);
        }

        [TestCase(1000, 10, 1000)]
        [TestCase(1000, 26, 1250)]
        [TestCase(1000, 27, 1250)]
        [TestCase(175, 26, 219)]
        [TestCase(140, 26, 175)]
        [TestCase(0, 26, 0)]
        public void ScaleByStat_AddsUpToTwentyFivePercentRoundedUp(int baseAmount, int stat, int expected)
        {
            HealingMath.ScaleByStat(baseAmount, stat).Should().Be(expected);
        }

        [Test]
        public void ScaleByStat_PercentHeals_MatchesTheOldTwentyFivePercentBonusForEveryAmountAndStat()
        {
            for (var baseAmount = 0; baseAmount <= 2000; baseAmount++)
            {
                for (var stat = 0; stat <= 30; stat++)
                {
                    var expected = baseAmount + (int)Math.Ceiling(baseAmount * 0.25f * HealingMath.StatProgress(stat));
                    HealingMath.ScaleByStat(baseAmount, stat, HealFormula.PercentHeals).Should().Be(expected, $"{baseAmount} at stat {stat}");
                }
            }
        }

        [Test]
        public void ScaleByStat_GameFormula_MatchesTheGamesRestorativeScalingForEveryAmountAndStat()
        {
            for (var baseAmount = 0; baseAmount <= 2000; baseAmount++)
            {
                for (var stat = 0; stat <= 30; stat++)
                {
                    HealingMath.ScaleByStat(baseAmount, stat, HealFormula.Game)
                        .Should().Be(AbilityEffectScaling.ScaleRestorativeEffect(baseAmount, stat), $"{baseAmount} at stat {stat}");
                }
            }
        }

        [TestCase(10, 625)]
        [TestCase(14, 782)]
        [TestCase(18, 938)]
        [TestCase(22, 1094)]
        [TestCase(26, 1250)]
        [TestCase(40, 1250)]
        public void ScaleByStat_GameFormula_HalvesTenWillpowerAgainstTwentySix(int stat, int expected)
        {
            HealingMath.ScaleByStat(1000, stat, HealFormula.Game).Should().Be(expected);
        }

        [TestCase(400, 153)]
        [TestCase(1000, 193)]
        [TestCase(1500, 228)]
        public void BenevolenceIII_OnAlly_UnderTheGameFormula_IsMostlyFlat(int targetMaxHp, int expected)
        {
            // (63 HP + 3.5% of max HP) x1.25 on an ally, then x1.25 at 26 Willpower and x1.25 Light affinity:
            // 400 HP: 97 -> 122 -> 153. 1,000 HP: 123 -> 154 -> 193. 1,500 HP: 145 -> 182 -> 228.
            HealingMath.DirectHeal(Effect(FeatType.Benevolence3), targetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, CurrentAffinityCap, 0f, 0f, HealFormula.Game).Should().Be(expected);
        }

        [Test]
        public void RenewalIII_Tick_UnderTheGameFormula_GrowsWithTheTickSizeAndAffinity()
        {
            // (14 HP + 0.75% of 1,000) x1.25 affinity = 26.875 -> 27, then x1.25 at 26 Willpower -> 34.
            HealingMath.HealOverTimeTick(Effect(FeatType.Renewal3), TargetMaxHp, MaxScalingWillpower, 1.25f, 0f, HealFormula.Game)
                .Should().Be(34);
        }

        [Test]
        public void DeflectorShieldIII_UnderTheGameFormula_Is154PlusThreePercent()
        {
            HealingMath.TemporaryHp(Effect(FeatType.DeflectorShield3), 1500, BaselineWillpower, 10, 0, CurrentAffinityCap, 0f, HealFormula.Game)
                .Should().Be(199);
        }

        [Test]
        public void RousingShoutIII_UnderTheGameFormula_RaisesBothPartsWithSocial()
        {
            // Halfway to SOC 26: 90 + round(11.5) = 102 HP plus 5.625% of 1,000 = 158.25 -> 159.
            HealingMath.TemporaryHp(Effect(FeatType.RousingShout3), TargetMaxHp, BaselineWillpower, 18, 0, CurrentAffinityCap, 0f, HealFormula.Game)
                .Should().Be(159);
        }

        [TestCase(0, 1.5f, 1f)]
        [TestCase(4, 1.5f, 1.2f)]
        [TestCase(LightAffinity, 1.5f, 1.5f)]
        [TestCase(LightAffinity, 1.25f, 1.25f)]
        [TestCase(-LightAffinity, 1.5f, 0.5f)]
        [TestCase(-20, 1.5f, 0.5f)]
        public void AffinityMultiplier_ScalesFivePercentPerPointWithinFloorAndCap(int affinity, float cap, float expected)
        {
            HealingMath.AffinityMultiplier(affinity, cap).Should().BeApproximately(expected, 1e-6f);
        }

        [TestCase(0f, 20)]
        [TestCase(10f, 22)]
        public void ForceSanctuaryPulse_GetsTheCastersOutgoingHealing(float outgoingHealingPercent, int expected)
        {
            // 2% of 1000 at baseline Willpower; the game's zone pulse heals through ApplyScaledHeal, which adds outgoing healing.
            HealingMath.PartyPulse(Effect(FeatType.ForceSanctuary1), TargetMaxHp, BaselineWillpower, LightAffinity,
                PreTuningAffinityCap, outgoingHealingPercent, 0f).Should().Be(expected);
        }

        [Test]
        public void BenevolenceIII_OnAlly_UnderPreTuningAffinityCap_HealsThreeHundredTwentyNine()
        {
            // 14% x 1.25 = 175, +25% Willpower = 219, x1.5 Light affinity = 328.5. The game rounds half away from zero
            // (Perk.ApplyForceAffinityMagnitude), so the heal is 329.
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, PreTuningAffinityCap, 0f, 0f);

            heal.Should().Be(329);
        }

        [Test]
        public void BenevolenceIII_OnAlly_UnderCurrentAffinityCap_HealsTwoHundredSeventyFour()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(274);
        }

        [Test]
        public void BenevolenceIII_OnSelf_UnderPreTuningAffinityCap_HealsTwoHundredSixtyThree()
        {
            // 14% = 140, +25% Willpower = 175, x1.5 Light affinity = 262.5 rounded away from zero.
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, true,
                MaxScalingWillpower, LightAffinity, PreTuningAffinityCap, 0f, 0f);

            heal.Should().Be(263);
        }

        [Test]
        public void BenevolenceIII_OnSelf_UnderCurrentAffinityCap_HealsTwoHundredNineteen()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, true,
                MaxScalingWillpower, LightAffinity, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(219);
        }

        [Test]
        public void BenevolenceIII_WithoutAffinity_IgnoresTheAffinityCap()
        {
            var underCurrentCap = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, PreTuningAffinityCap, 0f, 0f);
            var underTunedCap = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, CurrentAffinityCap, 0f, 0f);

            underCurrentCap.Should().Be(219);
            underTunedCap.Should().Be(219);
        }

        [Test]
        public void MedKitIV_AtMaxWillpower_HealsThreeHundred()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, PreTuningAffinityCap, 0f, 0f);

            heal.Should().Be(300);
        }

        [Test]
        public void MedKitIV_WithTenPercentOutgoingHealing_HealsThreeHundredThirty()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, PreTuningAffinityCap, 10f, 0f);

            heal.Should().Be(330);
        }

        [Test]
        public void MedKitIV_AtBaselineWillpower_GetsNoStatBonus()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                BaselineWillpower, 0, PreTuningAffinityCap, 0f, 0f);

            heal.Should().Be(240);
        }

        [Test]
        public void MedKitIV_IgnoresForceAffinity()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, PreTuningAffinityCap, 0f, 0f);

            heal.Should().Be(300);
        }

        [Test]
        public void MedKitIV_WithCombatReadiness_AddsThePercentRoundedUp()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, PreTuningAffinityCap, 0f, 20f);

            heal.Should().Be(360);
        }

        [Test]
        public void EmergencyTriage_DoublesAtOrBelowItsHpThresholdOnly()
        {
            var triage = Effect(FeatType.EmergencyTriage1);

            var healthy = HealingMath.DirectHeal(triage, TargetMaxHp, 50f, false, BaselineWillpower, 0, PreTuningAffinityCap, 0f, 0f);
            var wounded = HealingMath.DirectHeal(triage, TargetMaxHp, 35f, false, BaselineWillpower, 0, PreTuningAffinityCap, 0f, 0f);

            healthy.Should().Be(180);
            wounded.Should().Be(360);
        }

        [Test]
        public void HealOverTimeTick_SplitsTheTotalAcrossTicksThenScalesAndAddsOutgoingHealing()
        {
            // Infusion II: 30% over 10 ticks = 3% = 30; +25% Willpower = 38; +10% outgoing healing = 42.
            var tick = HealingMath.HealOverTimeTick(Effect(FeatType.Infusion2), TargetMaxHp, MaxScalingWillpower, 1f, 10f);

            tick.Should().Be(42);
        }

        [Test]
        public void HealOverTimeTick_UsesTheAffinityCapturedWhenCast()
        {
            // Renewal III: 30% x1.25 captured affinity over 10 ticks = 3.75% = 38; +25% Willpower = 48.
            var tick = HealingMath.HealOverTimeTick(Effect(FeatType.Renewal3), TargetMaxHp, MaxScalingWillpower, 1.25f, 0f);

            tick.Should().Be(48);
        }

        [TestCase(101, -20f, 80)]
        [TestCase(100, -100f, 1)]
        [TestCase(100, 25f, 125)]
        public void AddReceivedPercent_MatchesTheGamesHealingReceivedAdjustment(int amount, float percent, int expected)
        {
            HealingMath.AddReceivedPercent(amount, percent).Should().Be(expected);
        }

        [TestCase(100, -50f, 100)]
        [TestCase(100, 0f, 100)]
        [TestCase(101, 10f, 112)]
        public void AddOutgoingPercent_IgnoresAdjustmentsAtOrBelowZero(int amount, float percent, int expected)
        {
            HealingMath.AddOutgoingPercent(amount, percent).Should().Be(expected);
        }

        [Test]
        public void HealOverTimeTick_AppliesAffinityToThePercentBeforeRoundingAndWillpower()
        {
            // Renewal I: 10% x1.25 over 10 ticks = 1.25% = 13; +25% Willpower = 17. Scaling first would give 16.
            var tick = HealingMath.HealOverTimeTick(Effect(FeatType.Renewal1), TargetMaxHp, MaxScalingWillpower, 1.25f, 0f);

            tick.Should().Be(17);
        }

        [Test]
        public void PartyPulse_KoltoMistIsOnePercentPerTickScaledByWillpower()
        {
            var pulse = HealingMath.PartyPulse(Effect(FeatType.KoltoMist1), TargetMaxHp, MaxScalingWillpower, 0, PreTuningAffinityCap, 0f, 0f);

            pulse.Should().Be(13);
        }

        [Test]
        public void TemporaryHp_DeflectorShieldIII_IsPercentPlusFlatWithCombatReadiness()
        {
            var shield = Effect(FeatType.DeflectorShield3);

            HealingMath.TemporaryHp(shield, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, PreTuningAffinityCap, 0f).Should().Be(220);
            HealingMath.TemporaryHp(shield, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, PreTuningAffinityCap, 10f).Should().Be(242);
        }

        [Test]
        public void TemporaryHp_GuardianWardIV_UsesWillpowerAndTheAffinityCap()
        {
            // 15% = 150, +25% Willpower = 188.
            var ward = Effect(FeatType.GuardianWard4);

            HealingMath.TemporaryHp(ward, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, PreTuningAffinityCap, 0f).Should().Be(282);
            HealingMath.TemporaryHp(ward, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, CurrentAffinityCap, 0f).Should().Be(235);
        }

        [TestCase(10, 200)]
        [TestCase(26, 250)]
        public void TemporaryHp_RousingShoutIII_ScalesBetweenItsRangeBySocial(int social, int expected)
        {
            var shout = Effect(FeatType.RousingShout3);

            HealingMath.TemporaryHp(shout, TargetMaxHp, BaselineWillpower, social, 0, PreTuningAffinityCap, 0f).Should().Be(expected);
        }

        [TestCase(10, 10f)]
        [TestCase(18, 12f)]
        [TestCase(26, 13f)]
        public void SocialScaledPercent_RoundsTheBonusAwayFromZero(int social, float expected)
        {
            HealingMath.SocialScaledPercent(10f, 13f, social).Should().Be(expected);
        }

        [TestCase(0, 0f, 0)]
        [TestCase(100, 0f, 100)]
        [TestCase(100, 10f, 110)]
        [TestCase(105, 10f, 116)]
        public void AddPercent_RoundsTheBonusUp(int amount, float percent, int expected)
        {
            HealingMath.AddPercent(amount, percent).Should().Be(expected);
        }

        [TestCase(1000, 24f, 240)]
        [TestCase(1, 0.1f, 1)]
        public void PercentOfMaxHp_RoundsUpAndNeverReturnsLessThanOne(int maxHp, float percent, int expected)
        {
            HealingMath.PercentOfMaxHp(maxHp, percent).Should().Be(expected);
        }
    }
}
