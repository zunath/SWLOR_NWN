using FluentAssertions;
using NUnit.Framework;
using SWLOR.CraftSim.Legacy;
using SWLOR.CraftSim.Progression;
using SWLOR.CraftSim.Tests.Support;
using SWLOR.Game.Server.Service;

namespace SWLOR.CraftSim.Tests.Progression
{
    /// <summary>The progression model's copied rules stay pinned to the server source they were read from.</summary>
    [TestFixture]
    public class ProgressionSourceTests
    {
        // Parallel work may add or remove a few lines above a rule; a changed rule still fails.
        private const int Tolerance = 40;

        public static IEnumerable<TestCaseData> References =>
            ProgressionSource.All.Select(reference => new TestCaseData(reference).SetArgDisplayNames(reference.Name));

        [TestCaseSource(nameof(References))]
        public void EveryAnchorIsStillInTheCitedSource(SourceReference reference)
        {
            var path = Path.Combine(SharedData.Root, reference.File.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(path).Should().BeTrue($"{reference.File} holds the rule '{reference.Name}'");

            var lines = File.ReadAllLines(path);
            var from = Math.Max(0, reference.FirstLine - 1 - Tolerance);
            var to = Math.Min(lines.Length - 1, reference.LastLine - 1 + Tolerance);
            var window = lines[from..(to + 1)].Select(l => l.Trim()).ToList();

            foreach (var anchor in reference.Anchors)
            {
                window.Any(line => line.Contains(anchor, StringComparison.Ordinal))
                    .Should().BeTrue($"'{anchor}' must appear within {Tolerance} lines of {reference.Citation} ({reference.Name}); the rule changed or moved");
            }
        }

        [Test]
        public void TheRegistryListsEveryReference()
        {
            var declared = typeof(ProgressionSource)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(f => f.FieldType == typeof(SourceReference))
                .Select(f => (SourceReference)f.GetValue(null))
                .ToList();

            ProgressionSource.All.Should().BeEquivalentTo(declared, options => options.WithoutStrictOrdering());
            ProgressionSource.All.Select(r => r.Name).Should().OnlyHaveUniqueItems();
            ProgressionSource.All.Select(r => r.Name).Should().NotIntersectWith(LegacySource.All.Select(r => r.Name));
        }

        [Test]
        public void TheCopiedTimingsMatchTheirSources()
        {
            // The defaults are the server's constants; the anchors above prove the constants, these prove the copies.
            var defaults = new ProgressionAssumptions();
            defaults.SecondsPerHarvest.Should().Be(5.0, "HarvesterItemDefinition uses .Delay(5f)");
            defaults.ScavengeVisitSeconds.Should().Be(2.0, "ScavengePoint played one 2 second search animation for the whole visit, before its one or two attempts");
            defaults.RefineSecondsPerBatch.Should().Be(6.0, "RefineryViewModel.RefiningDelaySeconds is 6f");
            ProgressionAssumptions.RoleplayXpPerHour.Should().Be(1500 * 2, "RoleplayXP.cs pays 1500 base XP once every 300 six-second heartbeats");
        }

        [Test]
        public void RefiningABatchMatchesTheStackLimitOfTheMaterialBaseItems()
        {
            var path = Path.Combine(SharedData.Root, "SWLOR_Haks", "sw_2da", "baseitems.2da");
            if (!File.Exists(path))
                Assert.Ignore("SWLOR_Haks/sw_2da is not checked out here.");

            var lines = File.ReadAllLines(path);
            var header = lines[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var stacking = Array.IndexOf(header, "Stacking") + 1;

            // The material base items (raw ore, refined ore, wood, gems, fibers, leather, electronics) hold 99.
            foreach (var baseItem in new[] { "526", "536", "538" })
            {
                var row = lines.Skip(3).Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)).First(c => c.Length > 0 && c[0] == baseItem);
                int.Parse(row[stacking]).Should().Be(new ProgressionAssumptions().RefineBatchUnits, $"base item {baseItem}");
            }
        }

        [Test]
        public void RankFiftyCostsTheXpTheOwnerQuotes()
        {
            Enumerable.Range(0, ProgressionAssumptions.MaximumRank).Sum(Skill.GetRequiredXP).Should().Be(411_125);
        }
    }
}
