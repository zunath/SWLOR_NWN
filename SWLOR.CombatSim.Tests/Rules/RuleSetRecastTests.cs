using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Rules;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Rules
{
    [TestFixture]
    public class RuleSetRecastTests
    {
        private AbilityCatalog _catalog;

        [OneTimeSetUp]
        public void LoadCatalog()
        {
            _catalog = AbilityCatalog.Load();
        }

        [TestCase(FeatType.Benevolence1)]
        [TestCase(FeatType.Benevolence3)]
        public void Tuned_SetsBenevolenceRecastToTenSeconds(FeatType feat)
        {
            RuleSets.Tuned().GetRecastSeconds(_catalog.Get(feat)).Should().Be(10f);
        }

        [TestCase(FeatType.MedKit1)]
        [TestCase(FeatType.MedKit4)]
        public void Tuned_SetsMedKitRecastToTenAndAHalfSeconds(FeatType feat)
        {
            RuleSets.Tuned().GetRecastSeconds(_catalog.Get(feat)).Should().Be(10.5f);
        }

        [TestCase(FeatType.Benevolence3)]
        [TestCase(FeatType.MedKit4)]
        [TestCase(FeatType.QuickDraw4)]
        [TestCase(FeatType.Resuscitation2)]
        public void CurrentRules_UseTheGameDataRecast(FeatType feat)
        {
            var ability = _catalog.Get(feat);

            RuleSets.Current().GetRecastSeconds(ability).Should().Be(ability.RecastSeconds);
        }

        [Test]
        public void Tuned_LeavesGroupsWithoutAnOverrideAtTheirGameDataRecast()
        {
            var quickDraw4 = _catalog.Get(FeatType.QuickDraw4);

            RuleSets.Tuned().GetRecastSeconds(quickDraw4).Should().Be(quickDraw4.RecastSeconds);
        }

        [Test]
        public void RecastOverrides_AreMatchedByRecastGroupName()
        {
            var rules = new RuleSet { RecastOverrides = new Dictionary<string, float> { ["QuickDraw"] = 3f } };

            rules.GetRecastSeconds(_catalog.Get(FeatType.QuickDraw4)).Should().Be(3f);
            rules.GetRecastSeconds(_catalog.Get(FeatType.MedKit4)).Should().Be(_catalog.Get(FeatType.MedKit4).RecastSeconds);
        }
    }
}
