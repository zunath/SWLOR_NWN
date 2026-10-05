using NUnit.Framework;
using SWLOR.Toolset.Domain.GameData.TwoDa;

namespace SWLOR.Toolset.Tests.GameData.TwoDa;

/// <summary>Requires every complete corpus table to load, while rejecting the documented headerless fragment.</summary>
public sealed class TwoDaCorpusQualificationTests
{
    private const string CorpusRootVariable = "SWLOR_TEST_HAKS_ROOT";
    private const string TableDirectory = "sw_2da";
    private const string HeaderlessFragment = "iprp_spells past";

    [Test]
    public void EveryCompleteCorpusTableLoadsThroughTheSharedConsumer()
    {
        var service = OpenSelectedCorpus();
        var names = service.GetTableNames().Order(StringComparer.Ordinal).ToArray();
        Assert.That(names.Length, Is.GreaterThan(500), "The selected full corpus is required.");
        Assert.That(names, Does.Contain(HeaderlessFragment), "The documented scratch fragment must be reported separately.");

        var failures = new List<string>();
        var completeTables = 0;
        foreach (var name in names.Where(name => name != HeaderlessFragment))
        {
            completeTables++;
            if (!service.TryGetTable(name, out var table) || table is null)
                failures.Add(name);
        }

        TestContext.Out.WriteLine($"Complete corpus tables: {completeTables}; failed: {failures.Count}; separately rejected fragment: {HeaderlessFragment}.");
        Assert.That(failures, Is.Empty, "A tolerated parse failure does not qualify a complete table: " + string.Join(", ", failures));
    }

    [Test]
    public void DocumentedHeaderlessFragmentIsRejectedAsUnavailable()
    {
        var service = OpenSelectedCorpus();
        var path = Path.Combine(service.DirectoryPath, HeaderlessFragment + ".2da");
        using var reader = File.OpenText(path);
        var firstLine = reader.ReadLine();
        Assert.That(firstLine, Is.Not.Null);
        Assert.That(firstLine!.Trim(), Is.Not.EqualTo("2DA V2.0"), "If this becomes a complete table, remove the explicit fragment exception.");
        Assert.That(service.TryGetTable(HeaderlessFragment, out var table), Is.False);
        Assert.That(table, Is.Null);
    }

    private static TwoDaService OpenSelectedCorpus()
    {
        var root = Support.ToolsetCorpusPaths.HaksRoot;
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException($"Select the read-only full HAK corpus with {CorpusRootVariable}.");
        return new TwoDaService(Path.Combine(Path.GetFullPath(root), TableDirectory));
    }
}
