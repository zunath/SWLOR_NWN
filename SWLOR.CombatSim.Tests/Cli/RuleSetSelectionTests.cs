using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Cli;
using SWLOR.CombatSim.Rules;

namespace SWLOR.CombatSim.Tests.Cli
{
    [TestFixture]
    public class RuleSetSelectionTests
    {
        [Test]
        public void NoRuleOptions_SelectEveryPreset()
        {
            RuleSetSelection.Resolve(new Dictionary<string, List<string>>())
                .Select(rules => rules.Name).Should().Equal(RuleSets.Names);
        }

        [Test]
        public void RulesNames_AreSplitOnCommasAndTrimmed()
        {
            var options = new Dictionary<string, List<string>> { ["--rules"] = new() { "pre-tuning, current" } };

            RuleSetSelection.Resolve(options).Select(rules => rules.Name).Should().Equal(RuleSets.PreTuningName, RuleSets.CurrentName);
        }

        [TestCase("")]
        [TestCase(",")]
        [TestCase(" , ")]
        public void ARulesListThatNamesNothing_IsRejected(string value)
        {
            var options = new Dictionary<string, List<string>> { ["--rules"] = new() { value } };

            var act = () => RuleSetSelection.Resolve(options);

            act.Should().Throw<ArgumentException>().WithMessage("*--rules*");
        }
    }
}
