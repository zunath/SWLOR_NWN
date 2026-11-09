using FluentAssertions;
using NUnit.Framework;
using SWLOR.FacadeRemoval;
using SWLOR.Game.Server.Tests.Composition;

namespace SWLOR.Game.Server.Tests.Tools;

/// <summary>
/// Keeps the facade removal data in step with the rest of the branch: <c>tools/Refactor/RemovedFacades.json</c> lists
/// exactly the facades of the services marked <c>"facadeRemoved": true</c> in <c>ConvertedServices.json</c>, the names
/// that are also namespace segments are written explicitly, and every handler of a removed facade is renamed to a line of
/// the handler order manifest.
/// </summary>
[TestFixture]
public class FacadeRemovalDataTests
{
    // Names a namespace hides from the imported GameServices property (Domain.Combat, Feature.AbilityDefinition.Mimicry, ...).
    private static readonly string[] HiddenNames = ["Combat", "Mimicry", "Performance", "Prospecting"];

    private static string Root => RepositoryLocator.Find(TestContext.CurrentContext.TestDirectory)!;

    private static IReadOnlyList<RemovedFacade> Data => RemovedFacadeList.Load(Path.Combine(Root, "tools", "Refactor", "RemovedFacades.json"));

    [Test]
    public void The_data_lists_exactly_the_facades_of_the_services_marked_facade_removed()
    {
        var marked = ConvertedServiceFixtures.Load()
            .Where(service => service.RemovedFacade is not null)
            .Select(service => service.RemovedFacade!)
            .OrderBy(name => name, StringComparer.Ordinal);

        Data.Select(facade => facade.FullName).Should().Equal(marked);
        Data.Should().Contain(facade => facade.Facade == "Taxi").And.Contain(facade => facade.Facade == "ServerTask");
    }

    [Test]
    public void A_name_that_is_also_a_namespace_segment_is_written_explicitly()
    {
        Data.Where(facade => HiddenNames.Contains(facade.Name)).Should().OnlyContain(facade => facade.Explicit,
            "a namespace of the same name hides the imported property, so callers write GameServices.<Name>");
    }

    [Test]
    public void Every_member_of_a_split_facade_is_a_member_of_its_owner()
    {
        var combat = Data.Single(facade => facade.Facade == "Combat");
        combat.IsSplit.Should().BeTrue("Combat's members went to its parts, its service and its rules");
        var assembly = typeof(SWLOR.Game.Server.Composition.CompositionRoot).Assembly;

        foreach (var member in combat.Members!)
        {
            var owner = member.IsAccessorProperty
                ? assembly.GetType($"SWLOR.Game.Server.Domain.Combat.I{member.Owner}Service", throwOnError: true)!
                : assembly.GetType(member.Owner, throwOnError: true)!;
            owner.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance)
                .Should().Contain(method => method.Name == member.Name,
                $"{member.Owner} holds {member.Name}, which the tool rewrites the call to");
        }

        foreach (var constant in combat.Constants)
            assembly.GetType(constant.Owner, throwOnError: true)!.GetField(constant.Name).Should().NotBeNull($"{constant.Owner} holds the constant {constant.Name}");
    }

    [Test]
    public void Every_handler_of_a_removed_facade_is_renamed_to_a_line_of_the_manifest()
    {
        var manifest = File.ReadAllLines(Path.Combine(Root, "SWLOR.Game.Server", "Core", "ScriptHandlerOrder.txt"))
            .Select(line => line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length == 2 && (parts[0] == "action" || parts[0] == "conditional"))
            .Select(parts => parts[1])
            .ToHashSet(StringComparer.Ordinal);
        var renames = ConvertedServiceFixtures.HandlerRenames()
            .Where(rename => Data.Any(facade => rename.Master.StartsWith(facade.FullName + ".", StringComparison.Ordinal)))
            .ToList();

        renames.Should().Contain(("SWLOR.Game.Server.Service.Taxi.LoadTaxiDestinations", "SWLOR.Game.Server.Domain.World.TaxiService.LoadTaxiDestinations"));
        renames.Should().OnlyContain(rename => manifest.Contains(rename.Branch) && !manifest.Contains(rename.Master),
            "the manifest line keeps its position and takes the instance method's name");
    }
}
