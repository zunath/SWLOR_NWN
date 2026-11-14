using System.Text;
using NUnit.Framework;
using SWLOR.Architecture.Tests.Debt;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// The Phase 5 debt guards (P5-0): eight measures of structural debt, each with a shrinking baseline under
/// <c>Baseline/Phase5</c>, and the generated <c>DebtReport.md</c>. The measures read the source of every project of the
/// solution (Roslyn: syntax for sizes, the compiler's binding for the static lookup, constructor services and content
/// names) and the compiled server (IL: domain cycles, static state). See <c>Debt/DebtMeasures.cs</c> for each measure's
/// definition, which every baseline file repeats in its header. Every compared count is the same in a Debug and a
/// Release build.
/// </summary>
/// <remarks>
/// <para><b>What fails.</b> A line that is not in the baseline, a count above the baseline (new debt), and a count below it
/// or a line that is gone (debt paid: regenerate in the same pull request, so it cannot come back).</para>
/// <para><b>Regenerating.</b> <c>$env:SWLOR_UPDATE_SNAPSHOTS = "1"; dotnet test SWLOR.Architecture.Tests --no-build</c>
/// rewrites every baseline and the report; review the diff like any snapshot.</para>
/// </remarks>
[TestFixture]
public class Phase5DebtTests
{
    private const string BaselineFolder = "SWLOR.Architecture.Tests/Baseline/Phase5";

    private static readonly string[] MeasureFiles =
    [
        DebtMeasures.StaticLookupsFile,
        DebtMeasures.LargeTypesFile,
        DebtMeasures.LongMethodsFile,
        DebtMeasures.ServicesPerTypeFile,
        DebtMeasures.DeferredResolutionFile,
        DebtMeasures.LogicToContentFile,
        DebtMeasures.DomainCyclesFile,
        DebtMeasures.StaticStateFile
    ];

    [Test]
    public void The_exemptions_are_well_formed_and_each_names_something_in_the_server()
    {
        var exemptions = ArchitectureProject.Exemptions;
        var problems = exemptions.Problems.Concat(exemptions.Unmatched(ArchitectureProject.Source.Server)).ToList();

        if (problems.Count > 0)
            Assert.Fail($"{BaselineFolder}/Exemptions.json has problems ({problems.Count}):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", problems)}");
    }

    [TestCaseSource(nameof(MeasureFiles))]
    public void The_debt_matches_its_baseline(string fileName)
    {
        var measure = ArchitectureProject.Debt.Measure(fileName);
        var path = Path.Combine(ArchitectureProject.DebtBaselineDirectory, fileName);
        TestContext.Progress.WriteLine($"{measure.Title}: {string.Join(" ", measure.Summary)}");

        if (ArchitectureProject.IsUpdating)
        {
            Write(path, DebtBaselineFile.Render(measure.Entries, measure.Header));
            return;
        }

        if (!File.Exists(path))
            Assert.Fail($"{BaselineFolder}/{fileName} does not exist.{RegenerateHelp()}");

        var comparison = DebtComparison.Compare(DebtBaselineFile.Parse(File.ReadAllText(path)), measure.Entries);
        if (!comparison.IsClean)
            Assert.Fail(comparison.Describe(measure.Title, $"{BaselineFolder}/{fileName}", DebtReport.UnitOf(measure), RegenerateCommand()));
    }

    [Test]
    public void The_debt_report_is_current()
    {
        var debt = ArchitectureProject.Debt;
        TestContext.Progress.WriteLine("Phase 5 debt scan: " + string.Join(", ", debt.Timings));
        var report = DebtReport.Render(debt.Measures, ArchitectureProject.Exemptions);
        var path = Path.Combine(ArchitectureProject.DebtBaselineDirectory, DebtReport.FileName);

        if (ArchitectureProject.IsUpdating)
        {
            Write(path, report);
            return;
        }

        if (!File.Exists(path) || DebtBaselineFile.Normalize(File.ReadAllText(path)) != report)
        {
            // The report also shows numbers the baselines do not compare (members per domain direction), so it is not a guard.
            Assert.Warn($"{BaselineFolder}/{DebtReport.FileName} is out of date.{RegenerateHelp()}");
        }
    }

    private static void Write(string path, string content)
    {
        if (File.Exists(path) && DebtBaselineFile.Normalize(File.ReadAllText(path)) == content)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(false));
        TestContext.Progress.WriteLine($"Written: {Path.GetFileName(path)}");
    }

    private static string RegenerateCommand() =>
        $"PowerShell: $env:{ArchitectureProject.UpdateVariable} = \"1\"; dotnet test SWLOR.Architecture.Tests --no-build" +
        Environment.NewLine +
        $"  bash:       {ArchitectureProject.UpdateVariable}=1 dotnet test SWLOR.Architecture.Tests --no-build";

    private static string RegenerateHelp() =>
        Environment.NewLine + "Regenerate (rewrites the baselines and the report) and review the diff:" + Environment.NewLine + "  " + RegenerateCommand();
}
