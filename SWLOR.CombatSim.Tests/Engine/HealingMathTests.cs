using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine
{
    [TestFixture]
    public class HealingMathTests
    {
        private const float CurrentAffinityCap = 1.5f;
        private const float TunedAffinityCap = 1.25f;
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
                CurrentAffinityCap, outgoingHealingPercent, 0f).Should().Be(expected);
        }

        [Test]
        public void BenevolenceIII_OnAlly_UnderCurrentAffinityCap_HealsThreeHundredTwentyNine()
        {
            // 14% x 1.25 = 175, +25% Willpower = 219, x1.5 Light affinity = 328.5. The game rounds half away from zero
            // (Perk.ApplyForceAffinityMagnitude), so the heal is 329.
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(329);
        }

        [Test]
        public void BenevolenceIII_OnAlly_UnderTunedAffinityCap_HealsTwoHundredSeventyFour()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, TunedAffinityCap, 0f, 0f);

            heal.Should().Be(274);
        }

        [Test]
        public void BenevolenceIII_OnSelf_UnderCurrentAffinityCap_HealsTwoHundredSixtyThree()
        {
            // 14% = 140, +25% Willpower = 175, x1.5 Light affinity = 262.5 rounded away from zero.
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, true,
                MaxScalingWillpower, LightAffinity, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(263);
        }

        [Test]
        public void BenevolenceIII_OnSelf_UnderTunedAffinityCap_HealsTwoHundredNineteen()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, true,
                MaxScalingWillpower, LightAffinity, TunedAffinityCap, 0f, 0f);

            heal.Should().Be(219);
        }

        [Test]
        public void BenevolenceIII_WithoutAffinity_IgnoresTheAffinityCap()
        {
            var underCurrentCap = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, CurrentAffinityCap, 0f, 0f);
            var underTunedCap = HealingMath.DirectHeal(Effect(FeatType.Benevolence3), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, TunedAffinityCap, 0f, 0f);

            underCurrentCap.Should().Be(219);
            underTunedCap.Should().Be(219);
        }

        [Test]
        public void MedKitIV_AtMaxWillpower_HealsThreeHundred()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(300);
        }

        [Test]
        public void MedKitIV_WithTenPercentOutgoingHealing_HealsThreeHundredThirty()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, CurrentAffinityCap, 10f, 0f);

            heal.Should().Be(330);
        }

        [Test]
        public void MedKitIV_AtBaselineWillpower_GetsNoStatBonus()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                BaselineWillpower, 0, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(240);
        }

        [Test]
        public void MedKitIV_IgnoresForceAffinity()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, LightAffinity, CurrentAffinityCap, 0f, 0f);

            heal.Should().Be(300);
        }

        [Test]
        public void MedKitIV_WithCombatReadiness_AddsThePercentRoundedUp()
        {
            var heal = HealingMath.DirectHeal(Effect(FeatType.MedKit4), TargetMaxHp, 100f, false,
                MaxScalingWillpower, 0, CurrentAffinityCap, 0f, 20f);

            heal.Should().Be(360);
        }

        [Test]
        public void EmergencyTriage_DoublesAtOrBelowItsHpThresholdOnly()
        {
            var triage = Effect(FeatType.EmergencyTriage1);

            var healthy = HealingMath.DirectHeal(triage, TargetMaxHp, 50f, false, BaselineWillpower, 0, CurrentAffinityCap, 0f, 0f);
            var wounded = HealingMath.DirectHeal(triage, TargetMaxHp, 35f, false, BaselineWillpower, 0, CurrentAffinityCap, 0f, 0f);

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
            // Renewal III: 30% over 10 ticks = 30; +25% Willpower = 38; x1.25 captured affinity = 47.5, which rounds to 48.
            var tick = HealingMath.HealOverTimeTick(Effect(FeatType.Renewal3), TargetMaxHp, MaxScalingWillpower, 1.25f, 0f);

            tick.Should().Be(48);
        }

        [Test]
        public void PartyPulse_KoltoMistIsOnePercentPerTickScaledByWillpower()
        {
            var pulse = HealingMath.PartyPulse(Effect(FeatType.KoltoMist1), TargetMaxHp, MaxScalingWillpower, 0, CurrentAffinityCap, 0f, 0f);

            pulse.Should().Be(13);
        }

        [Test]
        public void TemporaryHp_DeflectorShieldIII_IsPercentPlusFlatWithCombatReadiness()
        {
            var shield = Effect(FeatType.DeflectorShield3);

            HealingMath.TemporaryHp(shield, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, CurrentAffinityCap, 0f).Should().Be(220);
            HealingMath.TemporaryHp(shield, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, CurrentAffinityCap, 10f).Should().Be(242);
        }

        [Test]
        public void TemporaryHp_GuardianWardIV_UsesWillpowerAndTheAffinityCap()
        {
            // 15% = 150, +25% Willpower = 188.
            var ward = Effect(FeatType.GuardianWard4);

            HealingMath.TemporaryHp(ward, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, CurrentAffinityCap, 0f).Should().Be(282);
            HealingMath.TemporaryHp(ward, TargetMaxHp, MaxScalingWillpower, 10, LightAffinity, TunedAffinityCap, 0f).Should().Be(235);
        }

        [TestCase(10, 200)]
        [TestCase(26, 250)]
        public void TemporaryHp_RousingShoutIII_ScalesBetweenItsRangeBySocial(int social, int expected)
        {
            var shout = Effect(FeatType.RousingShout3);

            HealingMath.TemporaryHp(shout, TargetMaxHp, BaselineWillpower, social, 0, CurrentAffinityCap, 0f).Should().Be(expected);
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
