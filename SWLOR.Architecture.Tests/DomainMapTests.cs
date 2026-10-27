using NUnit.Framework;
using SWLOR.Architecture.Tests.Domains;

namespace SWLOR.Architecture.Tests;

/// <summary>
/// Guards <c>DomainMap.json</c>: it must be well formed and must cover exactly the services and Feature folders of
/// <c>SWLOR.Game.Server</c>, so the dependency guard never silently ignores a new part of the codebase.
/// </summary>
[TestFixture]
public class DomainMapTests
{
    private const string MapName = "SWLOR.Architecture.Tests/DomainMap.json";

    [Test]
    public void Domain_map_file_is_well_formed()
    {
        var map = DomainMap.Load(ArchitectureProject.DomainMapPath);

        if (map.Problems.Count > 0)
            Assert.Fail($"{MapName} has {map.Problems.Count} problem(s):{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", map.Problems)}");
    }

    [Test]
    public void Every_service_and_feature_folder_is_in_a_domain()
    {
        var unmapped = ArchitectureProject.Server.UnmappedUnits;
        if (unmapped.Count == 0)
            return;

        var lines = unmapped.Select(unit => unit.Kind == UnitKind.Service
            ? $"  service {unit}  (add to 'services')"
            : $"  Feature folder {unit.Name}  (add to 'featureFolders')");
        Assert.Fail(
            $"{unmapped.Count} service(s) or Feature folder(s) are in no domain: add it to the domain map." + Environment.NewLine +
            string.Join(Environment.NewLine, lines) + Environment.NewLine +
            $"Edit {MapName} and put each one under the domain it belongs to. " +
            "A service is the static class Service.<Name> plus the types in Service.<Name>Service.*; " +
            "a Feature folder is the first folder below Feature. " +
            "Then regenerate the baseline if the new unit depends on another domain.");
    }

    [Test]
    public void Domain_map_lists_only_units_that_exist()
    {
        var existing = ArchitectureProject.Server.Units.ToHashSet();
        var stale = ArchitectureProject.Server.Map.ListedUnits
            .Where(unit => !existing.Contains(unit))
            .OrderBy(unit => unit.ToString(), StringComparer.Ordinal)
            .ToList();
        if (stale.Count == 0)
            return;

        Assert.Fail(
            $"{stale.Count} entry(ies) of {MapName} match no service or Feature folder of SWLOR.Game.Server any more: " +
            string.Join(", ", stale) + Environment.NewLine +
            "Remove them, or fix the name if the unit was renamed.");
    }
}
