using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Effects;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Effects
{
    [TestFixture]
    public class EffectCatalogTests
    {
        private static IEnumerable<(FeatType Feat, AbilityEffect Effect)> AllEffects() =>
            Enum.GetValues<FeatType>()
                .Distinct()
                .Where(feat => EffectCatalog.TryGet(feat, out _))
                .Select(feat =>
                {
                    EffectCatalog.TryGet(feat, out var effect);
                    return (feat, effect);
                });

        private static AbilityEffect Get(FeatType feat)
        {
            EffectCatalog.TryGet(feat, out var effect).Should().BeTrue();
            return effect;
        }

        [Test]
        public void TryGet_ForAnUnmodeledFeat_IsFalse()
        {
            EffectCatalog.TryGet(FeatType.Invalid, out var effect).Should().BeFalse();
            effect.Should().BeNull();
        }

        [Test]
        public void BenevolenceIII_IsAFourteenPercentHealThatIsStrongerOnAllies()
        {
            var effect = Get(FeatType.Benevolence3);

            effect.Kind.Should().Be(EffectKind.DirectHeal);
            effect.Percent.Should().Be(14f);
            effect.AllyMultiplier.Should().Be(1.25f);
            effect.UsesForceAffinity.Should().BeTrue();
            effect.UsesCombatReadiness.Should().BeTrue();
            effect.Scaling.Should().Be(ScalingStat.Willpower);
        }

        [TestCase(FeatType.MedKit1, 8f)]
        [TestCase(FeatType.MedKit2, 14f)]
        [TestCase(FeatType.MedKit3, 20f)]
        [TestCase(FeatType.MedKit4, 24f)]
        public void MedKit_HealsItsPercentWithoutForceAffinity(FeatType feat, float percent)
        {
            var effect = Get(feat);

            effect.Kind.Should().Be(EffectKind.DirectHeal);
            effect.Percent.Should().Be(percent);
            effect.UsesForceAffinity.Should().BeFalse();
            effect.AllyMultiplier.Should().Be(1f);
        }

        [Test]
        public void RenewalAndInfusion_ShareOneHealOverTimeSlot_AndOnlyRenewalUsesAffinity()
        {
            var renewal = Get(FeatType.Renewal3);
            var infusion = Get(FeatType.Infusion2);

            renewal.Key.Should().Be(infusion.Key, "one regenerative heal replaces the other");
            renewal.UsesForceAffinity.Should().BeTrue();
            infusion.UsesForceAffinity.Should().BeFalse();
            renewal.Ticks.Should().Be(10);
            renewal.TickSeconds.Should().Be(3f);
        }

        [Test]
        public void ShieldRanksOfOneAbility_ShareAKey_AndDifferentAbilitiesDoNot()
        {
            Get(FeatType.GuardianWard1).Key.Should().Be(Get(FeatType.GuardianWard4).Key);
            Get(FeatType.PainSuppressant1).Key.Should().Be(Get(FeatType.PainSuppressant2).Key);
            Get(FeatType.DeflectorShield1).Key.Should().Be(Get(FeatType.DeflectorShield3).Key);

            new[]
            {
                Get(FeatType.GuardianWard4).Key, Get(FeatType.PainSuppressant2).Key, Get(FeatType.DeflectorShield3).Key,
                Get(FeatType.GroupDeflector1).Key, Get(FeatType.RousingShout3).Key, Get(FeatType.CleanseOrder2).Key
            }.Should().OnlyHaveUniqueItems();
        }

        [Test]
        public void ResuscitationI_RevivesWithoutAHeal_AndResuscitationIIAddsATwentyPercentHeal()
        {
            Get(FeatType.Resuscitation1).Kind.Should().Be(EffectKind.Revive);
            Get(FeatType.Resuscitation1).Percent.Should().Be(0f);
            Get(FeatType.Resuscitation2).Kind.Should().Be(EffectKind.Revive);
            Get(FeatType.Resuscitation2).Percent.Should().Be(20f);
        }

        [Test]
        public void EveryDirectHeal_IsInstant()
        {
            AllEffects().Where(e => e.Effect.Kind == EffectKind.DirectHeal)
                .Should().OnlyContain(e => e.Effect.IsInstantHeal);
            AllEffects().Where(e => e.Effect.Kind != EffectKind.DirectHeal)
                .Should().OnlyContain(e => !e.Effect.IsInstantHeal);
        }

        [Test]
        public void EveryEffectThatLastsOrStacks_HasAKeyAndTimingTheEngineNeeds()
        {
            foreach (var (feat, effect) in AllEffects())
            {
                switch (effect.Kind)
                {
                    case EffectKind.HealOverTime:
                    case EffectKind.PartyHealOverTime:
                        effect.Key.Should().NotBeNullOrEmpty($"{feat} is stored by key");
                        effect.Ticks.Should().BeGreaterThan(0, feat.ToString());
                        effect.Percent.Should().BeGreaterThan(0, feat.ToString());
                        break;
                    case EffectKind.TemporaryHp:
                    case EffectKind.PartyTemporaryHp:
                        effect.Key.Should().NotBeNullOrEmpty($"{feat} is stored by key");
                        effect.DurationSeconds.Should().BeGreaterThan(0, feat.ToString());
                        break;
                    case EffectKind.DirectHeal:
                        effect.Percent.Should().BeGreaterThan(0, feat.ToString());
                        break;
                }
            }
        }

        [Test]
        public void SocialScaledShields_HaveARangeThatRisesWithSocial()
        {
            foreach (var (feat, effect) in AllEffects().Where(e => e.Effect.Scaling == ScalingStat.Social))
                effect.MaximumPercent.Should().BeGreaterThan(effect.Percent, feat.ToString());
        }
    }
}
