using System.Text;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The cross-domain dependency guard (refactor owner decision 9). It reads the compiled <c>SWLOR.Game.Server.dll</c>,
/// lifts every type reference to the service or Feature folder it comes from and goes to, and compares the edges
/// between different domains with the checked-in baseline.
/// </summary>
/// <remarks>
/// <para><b>What fails.</b> A cross-domain edge that is not in <c>Baseline/CrossDomainEdges.txt</c> (a new dependency),
/// and a baseline edge that no longer exists (the baseline is kept tight). Edges into the Core and Platform
/// domains are recorded in <c>Baseline/EdgesIntoCoreAndPlatform.txt</c> for information and never fail.</para>
/// <para><b>Regenerating.</b> After an intended change set <c>SWLOR_UPDATE_SNAPSHOTS=1</c> and run these tests again,
/// then review the diff of the baseline files like any snapshot. PowerShell:
/// <c>$env:SWLOR_UPDATE_SNAPSHOTS = "1"; dotnet test SWLOR.Architecture.Tests --no-build</c>.</para>
/// <para><b>Domains.</b> <c>DomainMap.json</c> maps services and Feature folders to domains. The guard maps types, not
/// files, so it keeps working when a later phase moves a domain's types into new folders and namespaces (extend
/// <see cref="UnitClassifier"/> when the namespaces change).</para>
/// <para><b>Limit.</b> The compiler inlines constants (enum members, <c>const</c> fields); a dependency made only
/// through one is invisible. See <c>AssemblyScanner</c>.</para>
/// </remarks>
[TestFixture]
public class CrossDomainDependencyTests
{
    private const string EnforcedBaselineName = "Baseline/CrossDomainEdges.txt";
    private const string UnrestrictedBaselineName = "Baseline/EdgesIntoCoreAndPlatform.txt";

    private static readonly string[] EnforcedHeader =
    [
        "Cross-domain dependencies of SWLOR.Game.Server, recorded by SWLOR.Architecture.Tests (refactor owner decision 9).",
        "One line per edge between two units of different domains: <from> -> <to> | <members>.",
        "A unit is a service (Combat), a Feature folder (Feature/PerkDefinition), a type directly in Feature (Feature.Auditing)",
        "or an area outside Service and Feature (Platform/Native). Domains come from DomainMap.json.",
        "<members> is the number of distinct members that hold the reference. It is information only and is never compared.",
        "Edges into the Core and Platform domains are not listed here; see EdgesIntoCoreAndPlatform.txt.",
        "The guard fails when an edge appears that is not listed, and when a listed edge no longer exists.",
        "Regenerate after an intended change: SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Architecture.Tests --no-build, then review the diff."
    ];

    private static readonly string[] UnrestrictedHeader =
    [
        "Edges from other domains into the Core and Platform domains (infrastructure everyone may use).",
        "Recorded for information only: the guard never fails on these, and warns when this file is out of date.",
        "Same format as CrossDomainEdges.txt. Regenerate with SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Architecture.Tests --no-build."
    ];

    [Test]
    public void No_new_cross_domain_dependency_and_none_has_disappeared()
    {
        var dependencies = ArchitectureProject.Server;
        var enforced = dependencies.EnforcedEdges.ToList();
        TestContext.Progress.WriteLine(DomainPairReport.Render("Cross-domain dependencies (guarded)", enforced));

        if (ArchitectureProject.IsUpdating)
        {
            RefuseToRecordUnmappedUnits(dependencies);
            Write(ArchitectureProject.EnforcedBaselinePath, EdgeBaselineFile.Render(enforced, EnforcedHeader), enforced.Count);
            return;
        }

        if (!File.Exists(ArchitectureProject.EnforcedBaselinePath))
            Assert.Fail($"{EnforcedBaselineName} does not exist.{RegenerateHelp()}");

        var baseline = EdgeBaselineFile.ParseKeys(File.ReadAllText(ArchitectureProject.EnforcedBaselinePath));
        var comparison = BaselineComparison.Compare(baseline, enforced);
        if (!comparison.IsClean)
            Assert.Fail(comparison.Describe(EnforcedBaselineName, RegenerateCommand()));
    }

    [Test]
    public void Edges_into_core_and_platform_are_recorded()
    {
        var dependencies = ArchitectureProject.Server;
        var unrestricted = dependencies.UnrestrictedEdges.ToList();
        TestContext.Progress.WriteLine(DomainPairReport.Render("Dependencies into Core and Platform (recorded, never failing)", unrestricted));

        if (ArchitectureProject.IsUpdating)
        {
            RefuseToRecordUnmappedUnits(dependencies);
            Write(ArchitectureProject.UnrestrictedBaselinePath, EdgeBaselineFile.Render(unrestricted, UnrestrictedHeader), unrestricted.Count);
            return;
        }

        if (!File.Exists(ArchitectureProject.UnrestrictedBaselinePath))
        {
            Assert.Warn($"{UnrestrictedBaselineName} does not exist.{RegenerateHelp()}");
            return;
        }

        var baseline = EdgeBaselineFile.ParseKeys(File.ReadAllText(ArchitectureProject.UnrestrictedBaselinePath));
        var comparison = BaselineComparison.Compare(baseline, unrestricted);
        if (!comparison.IsClean)
        {
            Assert.Warn(
                $"{UnrestrictedBaselineName} is out of date ({comparison.NewEdges.Count} new, {comparison.RemovedEdges.Count} gone). " +
                "Edges into Core and Platform never fail the guard." + RegenerateHelp());
        }
    }

    private static void RefuseToRecordUnmappedUnits(DomainDependencies dependencies)
    {
        if (dependencies.UnmappedUnits.Count > 0)
        {
            Assert.Fail(
                "Not rewriting the baseline: edges of these units would be missing. Add them to " +
                $"SWLOR.Architecture.Tests/DomainMap.json first: {string.Join(", ", dependencies.UnmappedUnits)}");
        }
    }

    private static void Write(string path, string content, int edgeCount)
    {
        if (File.Exists(path) && EdgeBaselineFile.Normalize(File.ReadAllText(path)) == content)
            return;

        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        TestContext.Progress.WriteLine($"Baseline written: {Path.GetFileName(path)} ({edgeCount} edges)");
    }

    private static string RegenerateCommand() =>
        $"PowerShell: $env:{ArchitectureProject.UpdateVariable} = \"1\"; dotnet test SWLOR.Architecture.Tests --no-build" +
        Environment.NewLine +
        $"  bash:       {ArchitectureProject.UpdateVariable}=1 dotnet test SWLOR.Architecture.Tests --no-build";

    private static string RegenerateHelp() =>
        Environment.NewLine + "Regenerate only for an intended, approved change:" + Environment.NewLine + "  " + RegenerateCommand();
}
