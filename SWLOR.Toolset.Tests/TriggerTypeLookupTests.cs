using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Toolset.Domain.Editors;
using SWLOR.Toolset.Editors;

namespace SWLOR.Toolset.Tests
{
    /// <summary>
    /// The trigger Type dropdown names the engine values it writes.
    /// </summary>
    /// <remarks>
    /// These were reversed: 1 was labelled "Trap" and 2 "Area Transition", so choosing either wrote the
    /// other one's value and silently changed the trigger's runtime kind. A wrong label here is not a
    /// cosmetic bug - it rewrites checked-in module data into the opposite object.
    /// <para>
    /// Asserted against the module's own blueprints as well as the constant, so the mapping cannot be
    /// "corrected" back without the corpus disagreeing.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class TriggerTypeLookupTests
    {
        private const int Generic = 0;
        private const int AreaTransition = 1;
        private const int Trap = 2;

        /// <summary>
        /// The trigger list is a hardcoded engine enum, so it needs none of the provider's optional
        /// game-data services - an otherwise bare provider still returns it.
        /// </summary>
        private static IReadOnlyList<LookupOption> Options() =>
            new LookupOptionProvider(
                    new Workspace.WorkspaceContext(
                        path => new Domain.Workspace.ModuleWorkspace(path),
                        new Workspace.OutputLogService()))
                .GetOptions(LookupKeys.TriggerTypes);

        [Test]
        public void TheThreeEngineValuesAreOffered()
        {
            var options = Options();

            options.Select(o => (int)o.Id).Should().Equal(Generic, AreaTransition, Trap);
        }

        [Test]
        public void TrapIsValueTwoAndAreaTransitionIsValueOne()
        {
            var options = Options();

            options.Single(o => o.Id == Trap).Display.Should().Be("Trap");
            options.Single(o => o.Id == AreaTransition).Display.Should().Be("Area Transition");
            options.Single(o => o.Id == Generic).Display.Should().Be("Generic");
        }

        [Test]
        public void TheModulesOwnTrapAgreesThatATrapIsTypeTwo()
        {
            // pitfalltrap carries TrapFlag=1, TrapDetectable=1, TrapType=122 and an empty LinkedTo - a
            // trap in every field - so whatever its Type is, that is the engine's trap value.
            var path = Path.Combine(CorpusLocator.ModuleDirectory, "utt", "pitfalltrap.utt.json");
            if (!File.Exists(path))
                Assert.Ignore("pitfalltrap.utt.json is not present in this checkout.");

            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            root.GetProperty("TrapFlag").GetProperty("value").GetInt32().Should().Be(1, "this blueprint is a trap");
            root.GetProperty("Type").GetProperty("value").GetInt32().Should().Be(Trap);

            Options().Single(o => o.Id == Trap).Display.Should().Be("Trap");
        }
    }
}
