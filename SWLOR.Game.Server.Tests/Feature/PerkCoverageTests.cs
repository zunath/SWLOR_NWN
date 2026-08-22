using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.EngineTests.Definitions.PerkCoverage;

namespace SWLOR.Game.Server.Tests.Feature
{
    /// <summary>
    /// Coverage ratchet for the perk catalog. PerkBuilder.Build() calls engine functions
    /// (2DA reads), so perks cannot be constructed in plain NUnit - this ratchet therefore
    /// source-scans the perk definitions for registered PerkTypes (the house pattern for
    /// perk tests) and enforces that every one has exactly one PerkCoverageCase. The
    /// case-vs-built structure comparison (levels, prices, granted feats, stat bonuses)
    /// runs inside the live engine in PerkSweepEngineTests, where Build() works for real.
    /// The orphan check is self-healing for scan gaps: perks registered through a wrapper
    /// this scan doesn't recognize surface as "orphaned" cases and force a scan update.
    /// </summary>
    public class PerkCoverageTests
    {
        private static readonly Regex DirectCreatePattern = new(
            @"\(PerkCategoryType\.\w+,\s*PerkType\.(\w+)",
            RegexOptions.Compiled);

        private static readonly Regex GrenadierWrapperPattern = new(
            @"CreateGrenadierPerk\(PerkType\.(\w+)\)",
            RegexOptions.Compiled);

        private static readonly Regex AbilityPerkReferencePattern = new(
            @"\.Create\(FeatType\.\w+,\s*PerkType\.(\w+)\)",
            RegexOptions.Compiled);

        private static HashSet<string> ScanRegisteredPerkNames()
        {
            var root = FindRepositoryRoot();
            var perkDefinitionDir = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "PerkDefinition");
            var names = new HashSet<string>();

            foreach (var file in Directory.EnumerateFiles(perkDefinitionDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                foreach (Match match in DirectCreatePattern.Matches(source))
                {
                    names.Add(match.Groups[1].Value);
                }

                foreach (Match match in GrenadierWrapperPattern.Matches(source))
                {
                    names.Add(match.Groups[1].Value);
                }
            }

            return names;
        }

        private static List<PerkCoverageCase> BuildAllCases()
        {
            var sourceTypes = typeof(IPerkCoverageSource).Assembly
                .GetTypes()
                .Where(t => typeof(IPerkCoverageSource).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            return sourceTypes
                .Select(t => (IPerkCoverageSource)Activator.CreateInstance(t))
                .SelectMany(s => s.BuildCases())
                .ToList();
        }

        [Test]
        public void EveryRegisteredPerkHasExactlyOneCoverageCase()
        {
            var registered = ScanRegisteredPerkNames();
            var cases = BuildAllCases();

            registered.Should().NotBeEmpty("the source scan must find the perk registrations");

            var duplicates = cases
                .GroupBy(c => c.Perk)
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key} (x{g.Count()})")
                .OrderBy(x => x)
                .ToList();
            duplicates.Should().BeEmpty("each perk must have exactly one coverage case");

            var covered = cases.Select(c => c.Perk.ToString()).ToHashSet();
            var missing = registered
                .Where(p => !covered.Contains(p))
                .OrderBy(p => p)
                .ToList();
            missing.Should().BeEmpty(
                "every registered perk must have a PerkCoverageCase - add one to its tree's IPerkCoverageSource");

            var orphaned = covered
                .Where(p => !registered.Contains(p))
                .OrderBy(p => p)
                .ToList();
            orphaned.Should().BeEmpty(
                "coverage cases must reference registered perks; if the perk IS registered through a new wrapper/helper, teach ScanRegisteredPerkNames the new pattern");
        }

        [Test]
        public void EveryAbilityPerkReferenceResolvesToARegisteredPerk()
        {
            // Static regression guard for the HackingBlade bug: an ability referencing a
            // perk with no definition crashes Perk.GetPerkLevel for NPC activators at runtime.
            var registered = ScanRegisteredPerkNames();
            var root = FindRepositoryRoot();
            var abilityDefinitionDir = Path.Combine(root.FullName, "SWLOR.Game.Server", "Feature", "AbilityDefinition");

            var badReferences = new List<string>();
            foreach (var file in Directory.EnumerateFiles(abilityDefinitionDir, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                foreach (Match match in AbilityPerkReferencePattern.Matches(source))
                {
                    var perkName = match.Groups[1].Value;
                    if (perkName != "Invalid" && !registered.Contains(perkName))
                    {
                        badReferences.Add($"{Path.GetFileName(file)}: PerkType.{perkName}");
                    }
                }
            }

            badReferences.Should().BeEmpty(
                "every ability's perk reference must be a registered perk (or Invalid) - unregistered references crash NPC perk-level lookups at runtime (the HackingBlade bug)");
        }

        [Test]
        public void CoverageCasesDeclareCoherentStructures()
        {
            // Cheap static sanity on the cases themselves; the authoritative comparison
            // against BUILT perk data runs in-engine (PerkSweepEngineTests).
            var failures = new List<string>();
            foreach (var coverageCase in BuildAllCases())
            {
                if (coverageCase.MaxLevel <= 0)
                {
                    failures.Add($"{coverageCase.Perk}: MaxLevel {coverageCase.MaxLevel} must be positive");
                }

                if (coverageCase.Prices.Length != coverageCase.MaxLevel)
                {
                    failures.Add($"{coverageCase.Perk}: {coverageCase.Prices.Length} price(s) declared for {coverageCase.MaxLevel} level(s)");
                }

                if (coverageCase.GrantedFeats.Distinct().Count() != coverageCase.GrantedFeats.Length)
                {
                    failures.Add($"{coverageCase.Perk}: GrantedFeats contains duplicates");
                }
            }

            failures.Should().BeEmpty("coverage cases must be internally coherent");
        }

        private static DirectoryInfo FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                {
                    return directory;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
        }
    }
}
