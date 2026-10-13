using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.CombatSim.Tests.Data
{
    [TestFixture]
    public class PerkCatalogTests
    {
        private static readonly StatType[] HarmonyStats =
        {
            StatType.HealingReceivedPercentAdjustment,
            StatType.HealingReceivedStaminaRestore,
            StatType.HealingReceivedStaminaRestoreChance,
            StatType.HealingReceivedStaminaRestoreChanceMaximum
        };

        private PerkCatalog _perks;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _perks = AbilityCatalog.Load().Perks;
        }

        [Test]
        public void DefensiveHarmony_GivesItsHealingReceivedAndStaminaRestoreStats()
        {
            var stats = _perks.GetStats(new[] { "DefensiveHarmony" }, HarmonyStats);

            stats.Should().Contain(StatType.HealingReceivedPercentAdjustment, 20)
                .And.Contain(StatType.HealingReceivedStaminaRestore, 8)
                .And.Contain(StatType.HealingReceivedStaminaRestoreChance, 10)
                .And.Contain(StatType.HealingReceivedStaminaRestoreChanceMaximum, 40);
        }

        [Test]
        public void ForceConvergence_SetsItsTriggerStat()
        {
            _perks.GetStats(new[] { "forceconvergence" }, new[] { StatType.ForceConvergence })
                .Should().Contain(StatType.ForceConvergence, 1);
        }

        [Test]
        public void GetStats_ReturnsOnlyTheRequestedStats()
        {
            _perks.GetStats(new[] { "DefensiveHarmony" }, new[] { StatType.ForceConvergence }).Should().BeEmpty();
        }

        [Test]
        public void APerkWithSeveralLevels_NeedsItsLevel()
        {
            var act = () => _perks.GetStats(new[] { "Benevolence" }, HarmonyStats);

            act.Should().Throw<InvalidDataException>().WithMessage("*Benevolence*3 levels*Benevolence:level*");
            _perks.Invoking(p => p.GetStats(new[] { "Benevolence:2" }, HarmonyStats)).Should().NotThrow();
        }

        [TestCase("Benevolence:9")]
        [TestCase("Benevolence:two")]
        [TestCase("NotARealPerk")]
        [TestCase("DefensiveHarmony:1:1")]
        public void GetStats_RejectsWhatItCannotRead(string perk)
        {
            var act = () => _perks.GetStats(new[] { perk }, HarmonyStats);

            act.Should().Throw<InvalidDataException>();
        }
    }
}
