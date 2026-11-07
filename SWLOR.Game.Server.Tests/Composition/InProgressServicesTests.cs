using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// The temporary <c>inProgress</c> list of <c>ConvertedServices.json</c>: <c>Combat</c> is split into part services one
/// layer at a time, and the guards that forbid a half-converted service read the list so each part can land alone.
/// P2-CB10 lists <c>Combat</c> under <c>services</c>, removes the list and deletes this fixture.
/// </summary>
[TestFixture]
public class InProgressServicesTests
{
    [Test]
    public void The_in_progress_list_holds_only_Combat()
    {
        ConvertedServiceFixtures.InProgressServices().Should().Equal(new[] { "Combat" },
            "no other service may be converted in pieces: convert it in one pull request and list it under services");
    }

    [TestCase("ICombatQueriesService", true)]
    [TestCase("ICombatDamageTakenService", true)]
    [TestCase("ICombatService", true)]
    [TestCase("ICombatPointService", false, Description = "listed on its own")]
    [TestCase("ICombatRecorderService", false, Description = "listed on its own")]
    [TestCase("IStealthService", false, Description = "another service")]
    [TestCase("IPvpCombatService", false, Description = "another service")]
    [TestCase("ICombatQueries", false, Description = "not a service interface")]
    [TestCase("CombatQueriesService", false, Description = "not an interface name")]
    public void A_part_of_Combat_is_named_after_it_and_not_a_service_already_listed(string interfaceName, bool isPart)
    {
        var listed = new HashSet<string>(StringComparer.Ordinal) { "ICombatPointService", "ICombatRecorderService" };

        ConvertedServiceFixtures.IsInProgressPartContract("Combat", interfaceName, listed).Should().Be(isPart);
    }

    [Test]
    public void A_service_that_is_not_in_progress_has_no_parts_by_this_pattern()
    {
        ConvertedServiceFixtures.IsInProgressPartContract("Stat", "ICombatQueriesService", new HashSet<string>())
            .Should().BeFalse();
    }

    [Test]
    public void The_listed_services_do_not_include_the_service_in_progress_itself()
    {
        ConvertedServiceFixtures.Load().Select(service => service.Name).Should().NotContain("Combat",
            "Combat is listed under services only when it is fully converted");
    }
}
