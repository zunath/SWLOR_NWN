using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the city governance: what the boot does to every city (citizen requirement, elections, level, upkeep and
/// citizenship fees), against an in-memory database that evaluates the queries the governance asks.
/// </summary>
[TestFixture]
public class PropertyCityGovernanceTests
{
    private const string Mayor = "mayor-id";
    private const string Rival = "rival-id";
    private const string Third = "third-id";

    private DateTime Now;

    private PropertyWorld _world = null!;

    private PropertyWorld.CityUnderTest City => _world.City;

    private InMemoryDBService Db => _world.Db;

    [SetUp]
    public void SetUp()
    {
        _world = new PropertyWorld();
        Now = DateTime.UtcNow;
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    /// <summary>A city whose upkeep and election are far in the future, owned by <see cref="Mayor"/>.</summary>
    private WorldProperty StoreCity(string name = "Test City", int level = 1, int upkeepOwed = 0)
    {
        var city = new WorldProperty
        {
            CustomName = name,
            PropertyType = PropertyType.City,
            OwnerPlayerId = Mayor,
            ParentPropertyId = "city_area",
            Layout = PropertyLayoutType.City,
            Upkeep = upkeepOwed
        };
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(5);
        city.Dates[PropertyDateType.ElectionStart] = Now.AddDays(10);
        city.Upgrades[PropertyUpgradeType.CityLevel] = level;
        city.Upgrades[PropertyUpgradeType.MedicalCenterLevel] = 1;
        city.Upgrades[PropertyUpgradeType.StarportLevel] = 1;
        city.Upgrades[PropertyUpgradeType.CantinaLevel] = 1;
        city.Taxes[PropertyTaxType.Citizenship] = 0;
        Db.With(city);
        return city;
    }

    private Player StorePlayer(string id, string cityId = "", int managementPerk = 0, int upkeepPerk = 0, bool deleted = false)
    {
        var player = new Player { Id = id, Name = "Player " + id, CitizenPropertyId = cityId, IsDeleted = deleted };
        if (managementPerk > 0)
            player.Perks[PerkType.CityManagement] = managementPerk;
        if (upkeepPerk > 0)
            player.Perks[PerkType.Upkeep] = upkeepPerk;
        Db.With(player);
        return player;
    }

    private void StoreCitizens(WorldProperty city, int count, bool includeMayor = true)
    {
        if (includeMayor)
            StorePlayer(Mayor, city.Id);
        for (var index = includeMayor ? 1 : 0; index < count; index++)
            StorePlayer("citizen-" + index, city.Id);
    }

    private WorldPropertyPermission StorePermission(string playerId, string propertyId, params PropertyPermissionType[] granted)
    {
        var permission = new WorldPropertyPermission { PlayerId = playerId, PropertyId = propertyId };
        foreach (var type in granted)
        {
            permission.Permissions[type] = true;
            permission.GrantPermissions[type] = true;
        }

        Db.With(permission);
        return permission;
    }

    private Election StoreElection(WorldProperty city, ElectionStageType stage, params string[] candidates)
    {
        var election = new Election { PropertyId = city.Id, Stage = stage };
        election.CandidatePlayerIds.AddRange(candidates);
        Db.With(election);
        return election;
    }

    private static void Vote(Election election, string voter, string candidate)
    {
        election.VoterSelections[voter] = new ElectionVoter { VoterPlayerId = voter, CandidatePlayerId = candidate };
    }

    /// <summary>Moves a city's election so that the registration window closed a day ago (voting is open).</summary>
    private void VotingOpen(WorldProperty city) =>
        city.Dates[PropertyDateType.ElectionStart] = Now.AddDays(-(PropertyCityGovernance.ElectionRegistrationDays + 1));

    /// <summary>Moves a city's election so that registration is closed and voting ended a day ago.</summary>
    private void VotingClosed(WorldProperty city) =>
        city.Dates[PropertyDateType.ElectionStart] = Now.AddDays(-(PropertyCityGovernance.ElectionRegistrationDays + PropertyCityGovernance.ElectionVotingDays + 1));

    private WorldProperty Saved(WorldProperty city) => Db.Saved.OfType<WorldProperty>().Last(x => x.Id == city.Id);

    // ---- the constants and the pure rules

    [Test]
    public void The_grace_period_and_election_windows_are_what_they_have_always_been()
    {
        PropertyCityGovernance.MinimumCitizensGracePeriodHours.Should().Be(18);
        PropertyCityGovernance.ElectionRegistrationDays.Should().Be(14);
        PropertyCityGovernance.ElectionVotingDays.Should().Be(7);
        PropertyCityGovernance.PropertyBroadcastColor.Should().Be(5763719);
    }

    [TestCase(1, "Outpost")]
    [TestCase(2, "Village")]
    [TestCase(3, "Township")]
    [TestCase(4, "City")]
    [TestCase(5, "Metropolis")]
    [TestCase(0, "")]
    [TestCase(6, "")]
    public void City_levels_have_names(int level, string name)
    {
        City.GetCityLevelName(level).Should().Be(name);
    }

    [TestCase(1, 5)]
    [TestCase(2, 10)]
    [TestCase(3, 15)]
    [TestCase(4, 20)]
    [TestCase(5, 25)]
    [TestCase(0, -1)]
    [TestCase(6, -1)]
    public void Each_city_level_needs_a_number_of_citizens(int level, int required)
    {
        City.GetCitizensRequiredForNextCityLevel(level).Should().Be(required);
    }

    [Test]
    public void Dates_are_shown_in_utc_to_the_minute()
    {
        City.FormatUtcDate(new DateTime(2026, 3, 4, 5, 6, 59, DateTimeKind.Utc)).Should().Be("2026-03-04 05:06 UTC");
    }

    [Test]
    public void Building_the_governance_touches_neither_the_engine_nor_the_database()
    {
        _world.ClearCalls();
        var db = new InMemoryDBService();

        _ = new PropertyCityGovernance(db, _world.Recorded);

        _world.Engine.Calls.Should().BeEmpty();
        db.Reads.Should().BeEmpty();
    }

    // ---- the effective upgrade level

    [Test]
    public void The_effective_upgrade_level_is_zero_without_a_city_or_an_upgrade()
    {
        var city = StoreCity();
        city.Upgrades.Remove(PropertyUpgradeType.CantinaLevel);

        City.GetEffectiveUpgradeLevel(string.Empty, PropertyUpgradeType.StarportLevel).Should().Be(0);
        City.GetEffectiveUpgradeLevel(" ", PropertyUpgradeType.StarportLevel).Should().Be(0);
        City.GetEffectiveUpgradeLevel("not-stored", PropertyUpgradeType.StarportLevel).Should().Be(0);
        City.GetEffectiveUpgradeLevel(city.Id, PropertyUpgradeType.CantinaLevel).Should().Be(0, "the city never bought it");
        City.GetEffectiveUpgradeLevel(city.Id, PropertyUpgradeType.CityLevel).Should().Be(0, "the city level itself has no building");
    }

    [TestCase(PropertyUpgradeType.MedicalCenterLevel, StructureType.MedicalCenter)]
    [TestCase(PropertyUpgradeType.StarportLevel, StructureType.Starport)]
    [TestCase(PropertyUpgradeType.CantinaLevel, StructureType.Cantina)]
    public void The_effective_upgrade_level_needs_a_building_and_is_capped_by_the_city_level(PropertyUpgradeType upgrade, StructureType building)
    {
        var city = StoreCity(level: 3);
        city.Upgrades[upgrade] = 5;
        City.GetEffectiveUpgradeLevel(city.Id, upgrade).Should().Be(0, "the city has no such building");

        var structure = new WorldProperty
        {
            PropertyType = PropertyType.Structure, StructureType = building, ParentPropertyId = city.Id
        };
        Db.With(structure);
        City.GetEffectiveUpgradeLevel(city.Id, upgrade).Should().Be(3, "the city level caps an upgrade above it");

        city.Upgrades[upgrade] = 2;
        City.GetEffectiveUpgradeLevel(city.Id, upgrade).Should().Be(2);

        structure.IsQueuedForDeletion = true;
        City.GetEffectiveUpgradeLevel(city.Id, upgrade).Should().Be(0, "a building queued for deletion does not count");
    }

    // ---- the citizen requirement

    [Test]
    public void A_city_that_falls_below_the_citizen_count_gets_a_grace_period()
    {
        var city = StoreCity();
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(5);
        StoreCitizens(city, 4);

        City.ProcessCities();

        var saved = Saved(city);
        saved.IsQueuedForDeletion.Should().BeFalse();
        saved.Dates[PropertyDateType.BelowRequiredCitizens].Should().BeCloseTo(DateTime.UtcNow.AddHours(PropertyCityGovernance.MinimumCitizensGracePeriodHours), TimeSpan.FromMinutes(1));
    }

    [Test]
    public void A_city_still_below_the_count_inside_its_grace_period_is_left_alone_and_one_past_it_is_queued_for_deletion()
    {
        var inGrace = StoreCity("In grace");
        inGrace.Dates[PropertyDateType.BelowRequiredCitizens] = Now.AddHours(10);
        var expired = StoreCity("Expired");
        expired.Dates[PropertyDateType.BelowRequiredCitizens] = Now.AddHours(-1);
        StorePlayer("only-one-in-grace", inGrace.Id);
        StorePlayer("only-one-in-expired", expired.Id);

        City.ProcessCities();

        Saved(inGrace).IsQueuedForDeletion.Should().BeFalse();
        Saved(inGrace).Dates.Should().ContainKey(PropertyDateType.BelowRequiredCitizens);
        Saved(expired).IsQueuedForDeletion.Should().BeTrue();
    }

    [Test]
    public void A_city_with_enough_citizens_loses_its_grace_period_and_deleted_citizens_do_not_count()
    {
        var city = StoreCity();
        city.Dates[PropertyDateType.BelowRequiredCitizens] = Now.AddHours(10);
        StoreCitizens(city, 5);

        City.ProcessCities();

        Saved(city).Dates.Should().NotContainKey(PropertyDateType.BelowRequiredCitizens);

        var other = StoreCity("Other");
        StoreCitizens(other, 1, includeMayor: false);
        for (var index = 0; index < 5; index++)
            StorePlayer("gone-" + index, other.Id, deleted: true);

        City.ProcessCities();

        Saved(other).Dates.Should().ContainKey(PropertyDateType.BelowRequiredCitizens, "five deleted players are not citizens");
    }

    [Test]
    public void A_city_queued_for_deletion_is_skipped()
    {
        var doomed = StoreCity("Doomed");
        doomed.IsQueuedForDeletion = true;

        City.ProcessCities();

        Db.Saved.Should().BeEmpty("the query leaves out cities queued for deletion");
    }

    [Test]
    public void A_city_that_expires_is_not_processed_further()
    {
        var city = StoreCity();
        city.Dates[PropertyDateType.BelowRequiredCitizens] = Now.AddHours(-1);
        city.Dates[PropertyDateType.ElectionStart] = Now.AddDays(-1);
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        StorePlayer(Mayor, city.Id);

        City.ProcessCities();

        Db.Saved.OfType<Election>().Should().BeEmpty("no election is opened for a city that is going away");
        Saved(city).Upkeep.Should().Be(0, "no upkeep is assessed either");
    }

    // ---- elections

    [Test]
    public void An_election_opens_when_the_date_is_reached()
    {
        var city = StoreCity();
        city.Dates[PropertyDateType.ElectionStart] = Now.AddMinutes(-1);
        StoreCitizens(city, 5);

        City.ProcessCities();

        var election = Db.Saved.OfType<Election>().Should().ContainSingle().Subject;
        election.PropertyId.Should().Be(city.Id);
        election.Stage.Should().Be(ElectionStageType.Registration);
    }

    [Test]
    public void No_election_opens_before_its_date()
    {
        var city = StoreCity();
        StoreCitizens(city, 5);

        City.ProcessCities();

        Db.Saved.OfType<Election>().Should().BeEmpty();
    }

    [Test]
    public void An_election_with_nobody_standing_ends_with_the_incumbent_and_the_next_one_is_scheduled()
    {
        var city = StoreCity();
        VotingOpen(city);
        var start = city.Dates[PropertyDateType.ElectionStart];
        StoreCitizens(city, 5);
        var election = StoreElection(city, ElectionStageType.Registration);

        City.ProcessCities();

        Db.Deleted.Should().Contain(election.Id);
        Saved(city).Dates[PropertyDateType.ElectionStart].Should().Be(start.AddDays(PropertyCityGovernance.ElectionRegistrationDays).AddDays(21));
        Saved(city).OwnerPlayerId.Should().Be(Mayor);
    }

    [Test]
    public void A_single_candidate_who_is_a_newcomer_becomes_mayor_with_the_mayors_permissions()
    {
        var city = StoreCity();
        VotingOpen(city);
        StoreCitizens(city, 5);
        var mayorPermission = StorePermission(Mayor, city.Id, PropertyPermissionType.EditTaxes);
        var election = StoreElection(city, ElectionStageType.Registration, Rival);

        City.ProcessCities();

        Saved(city).OwnerPlayerId.Should().Be(Rival);
        Db.Deleted.Should().Contain(mayorPermission.Id).And.Contain(election.Id);
        var rivalPermission = Db.Saved.OfType<WorldPropertyPermission>().Single(x => x.PlayerId == Rival);
        rivalPermission.PropertyId.Should().Be(city.Id);
        rivalPermission.Permissions.Keys.Should().BeEquivalentTo(_world.Catalog.GetPermissionsByPropertyType(PropertyType.City));
        rivalPermission.GrantPermissions.Values.Should().OnlyContain(granted => granted);
    }

    [Test]
    public void A_single_candidate_who_is_already_mayor_keeps_the_office_untouched()
    {
        var city = StoreCity();
        VotingOpen(city);
        StoreCitizens(city, 5);
        StoreElection(city, ElectionStageType.Registration, Mayor);

        City.ProcessCities();

        Saved(city).OwnerPlayerId.Should().Be(Mayor);
        Db.Saved.OfType<WorldPropertyPermission>().Should().BeEmpty();
        Db.Deleted.Should().ContainSingle();
    }

    [Test]
    public void An_election_with_several_candidates_moves_to_voting_when_registration_closes()
    {
        var city = StoreCity();
        VotingOpen(city);
        StoreCitizens(city, 5);
        var election = StoreElection(city, ElectionStageType.Registration, Mayor, Rival);

        City.ProcessCities();

        election.Stage.Should().Be(ElectionStageType.Voting);
        Db.Saved.OfType<Election>().Should().ContainSingle().Which.Should().BeSameAs(election);
        Db.Deleted.Should().BeEmpty();
    }

    [Test]
    public void An_election_still_in_registration_is_left_alone()
    {
        var city = StoreCity();
        city.Dates[PropertyDateType.ElectionStart] = Now.AddDays(-3);
        StoreCitizens(city, 5);
        var election = StoreElection(city, ElectionStageType.Registration, Mayor, Rival);

        City.ProcessCities();

        election.Stage.Should().Be(ElectionStageType.Registration);
        Db.Saved.OfType<Election>().Should().BeEmpty();
    }

    [Test]
    public void The_candidate_with_the_most_votes_wins_and_takes_over_the_mayors_permissions()
    {
        var city = StoreCity();
        VotingClosed(city);
        var start = city.Dates[PropertyDateType.ElectionStart];
        StoreCitizens(city, 5);
        var mayorPermission = StorePermission(Mayor, city.Id, PropertyPermissionType.EditTaxes);
        var rivalPermission = StorePermission(Rival, city.Id);
        var election = StoreElection(city, ElectionStageType.Voting, Mayor, Rival, Third);
        Vote(election, "v1", Rival);
        Vote(election, "v2", Rival);
        Vote(election, "v3", Mayor);

        City.ProcessCities();

        Saved(city).OwnerPlayerId.Should().Be(Rival);
        Db.Deleted.Should().Contain(mayorPermission.Id).And.Contain(election.Id);
        Db.Saved.OfType<WorldPropertyPermission>().Should().ContainSingle().Which.Should().BeSameAs(rivalPermission, "an existing permission record is reused");
        rivalPermission.Permissions[PropertyPermissionType.EditTaxes].Should().BeTrue();
        Saved(city).Dates[PropertyDateType.ElectionStart].Should().Be(start.AddDays(PropertyCityGovernance.ElectionRegistrationDays + PropertyCityGovernance.ElectionVotingDays).AddDays(21));
    }

    [Test]
    public void A_tie_for_first_place_keeps_the_incumbent()
    {
        var city = StoreCity();
        VotingClosed(city);
        StoreCitizens(city, 5);
        var election = StoreElection(city, ElectionStageType.Voting, Mayor, Rival);
        Vote(election, "v1", Rival);
        Vote(election, "v2", Mayor);

        City.ProcessCities();

        Saved(city).OwnerPlayerId.Should().Be(Mayor);
        Db.Deleted.Should().ContainSingle().Which.Should().Be(election.Id);
    }

    [Test]
    public void An_election_nobody_voted_in_keeps_the_incumbent()
    {
        var city = StoreCity();
        VotingClosed(city);
        StoreCitizens(city, 5);
        var election = StoreElection(city, ElectionStageType.Voting, Mayor, Rival);

        City.ProcessCities();

        Saved(city).OwnerPlayerId.Should().Be(Mayor);
        Db.Deleted.Should().ContainSingle().Which.Should().Be(election.Id);
    }

    // ---- level, upkeep and fees

    [Test]
    public void A_city_whose_upkeep_is_not_due_yet_is_not_assessed()
    {
        var city = StoreCity();
        StoreCitizens(city, 5);
        StorePlayer(Mayor, city.Id, managementPerk: 4);

        City.ProcessCities();

        Saved(city).Upkeep.Should().Be(0);
        Saved(city).Dates[PropertyDateType.Upkeep].Should().BeCloseTo(Now.AddDays(5), TimeSpan.FromSeconds(1));
    }

    private WorldProperty DueCity(int level = 1, int upkeepOwed = 0, int citizens = 5, int managementPerk = 1, int upkeepPerk = 0)
    {
        var city = StoreCity(level: level, upkeepOwed: upkeepOwed);
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        city.Taxes[PropertyTaxType.Citizenship] = 30;
        StoreCitizens(city, citizens, includeMayor: false);
        StorePlayer(Mayor, city.Id, managementPerk, upkeepPerk);
        return city;
    }

    [Test]
    public void A_due_city_with_paid_upkeep_that_meets_the_next_level_is_upgraded_and_billed()
    {
        var city = DueCity(level: 1, citizens: 10, managementPerk: 4);

        City.ProcessCities();

        var saved = Saved(city);
        saved.Upgrades[PropertyUpgradeType.CityLevel].Should().Be(2);
        saved.Upkeep.Should().Be(_world.Catalog.GetLayoutByType(PropertyLayoutType.City).PricePerDay * 7);
        saved.Dates[PropertyDateType.Upkeep].Should().BeCloseTo(Now.AddDays(-1).AddDays(7), TimeSpan.FromSeconds(1));
        Db.Saved.OfType<Player>().Should().HaveCount(11, "each of the eleven citizens is charged once, the mayor included");
        Db.Saved.OfType<Player>().Should().OnlyContain(citizen => citizen.PropertyOwedTaxes == 30);
    }

    [Test]
    public void A_city_that_has_not_paid_cannot_grow_and_a_mayor_with_a_lower_perk_lowers_the_level()
    {
        var unpaid = DueCity(level: 2, upkeepOwed: 500, citizens: 10, managementPerk: 4);
        var weakMayor = StoreCity("Weak mayor", level: 4);
        weakMayor.OwnerPlayerId = "weak-mayor";
        weakMayor.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        StorePlayer("weak-mayor", weakMayor.Id, managementPerk: 2);
        for (var index = 0; index < 25; index++)
            StorePlayer("weak-citizen-" + index, weakMayor.Id);

        City.ProcessCities();

        Saved(unpaid).Upgrades[PropertyUpgradeType.CityLevel].Should().Be(2, "upkeep is owed, so no level is gained");
        Saved(weakMayor).Upgrades[PropertyUpgradeType.CityLevel].Should().Be(3, "the mayor's perk supports level 3 at most");
    }

    [Test]
    public void A_city_without_enough_citizens_drops_to_the_level_they_support()
    {
        var city = DueCity(level: 4, citizens: 12, managementPerk: 5);

        City.ProcessCities();

        Saved(city).Upgrades[PropertyUpgradeType.CityLevel].Should().Be(2, "twelve citizens support level 2");
    }

    [Test]
    public void Upkeep_is_reduced_by_the_mayors_upkeep_perk_and_grows_with_the_upgrades()
    {
        var plain = DueCity(upkeepPerk: 0);
        var discounted = StoreCity("Discounted");
        discounted.OwnerPlayerId = "discount-mayor";
        discounted.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        discounted.Upgrades[PropertyUpgradeType.StarportLevel] = 3;
        discounted.Upgrades[PropertyUpgradeType.CantinaLevel] = 2;
        StorePlayer("discount-mayor", discounted.Id, managementPerk: 1, upkeepPerk: 4);
        for (var index = 0; index < 4; index++)
            StorePlayer("discount-citizen-" + index, discounted.Id);

        City.ProcessCities();

        var weekly = _world.Catalog.GetLayoutByType(PropertyLayoutType.City).PricePerDay * 7;
        Saved(plain).Upkeep.Should().Be(weekly);
        var expected = weekly - (int)(weekly * (4 * 0.05f)) + (3 - 1) * 10000 + (2 - 1) * 10000;
        Saved(discounted).Upkeep.Should().Be(expected);
    }

    [Test]
    public void Unpaid_upkeep_starts_a_countdown_that_ends_the_city_and_paid_upkeep_clears_it()
    {
        var first = DueCity(upkeepOwed: 100);
        var warned = StoreCity("Warned", upkeepOwed: 100);
        warned.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        warned.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(10);
        warned.OwnerPlayerId = "warned-mayor";
        StorePlayer("warned-mayor", warned.Id, managementPerk: 1);
        var doomed = StoreCity("Doomed", upkeepOwed: 100);
        doomed.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        doomed.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(-1);
        doomed.OwnerPlayerId = "doomed-mayor";
        StorePlayer("doomed-mayor", doomed.Id, managementPerk: 1);
        var paid = StoreCity("Paid");
        paid.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        paid.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(10);
        paid.OwnerPlayerId = "paid-mayor";
        StorePlayer("paid-mayor", paid.Id, managementPerk: 1);

        City.ProcessCities();

        Saved(first).Dates[PropertyDateType.DisrepairDestruction].Should().BeCloseTo(Now.AddDays(30), TimeSpan.FromMinutes(1));
        Saved(warned).Dates[PropertyDateType.DisrepairDestruction].Should().BeCloseTo(Now.AddDays(10), TimeSpan.FromSeconds(1));
        Saved(doomed).IsQueuedForDeletion.Should().BeTrue();
        Saved(paid).Dates.Should().NotContainKey(PropertyDateType.DisrepairDestruction);
    }

    [Test]
    public void A_city_that_runs_out_of_upkeep_time_is_marked_for_removal_before_it_is_billed_again()
    {
        var doomed = StoreCity("Doomed", upkeepOwed: 100);
        doomed.Dates[PropertyDateType.Upkeep] = Now.AddDays(-1);
        doomed.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(-1);
        doomed.Taxes[PropertyTaxType.Citizenship] = 30;
        StoreCitizens(doomed, 5);

        City.ProcessCities();

        Saved(doomed).IsQueuedForDeletion.Should().BeTrue();
        Saved(doomed).Upkeep.Should().Be(100, "the city was marked for removal before it was billed again");
        Db.Saved.OfType<Player>().Should().HaveCount(5, "the citizenship fees are still charged for the cycle");
        Db.Saved.OfType<Player>().Should().OnlyContain(citizen => citizen.PropertyOwedTaxes == 30);
    }

    // ---- broadcasts

    [Test]
    public void A_broadcast_without_a_webhook_does_nothing()
    {
        var city = StoreCity();
        _world.ClearCalls();

        City.BroadcastPropertyEvent(city, "Title", "Details");
        City.BroadcastCityUpkeepPaid(city);

        _world.Engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void Broadcasts_with_a_webhook_are_queued_without_failing_the_caller()
    {
        var city = StoreCity("Name @everyone `x`");
        City.WebhookUrl = "https://example.invalid/hook";

        var broadcast = () =>
        {
            City.BroadcastPropertyEvent(city, "Title @here", "Details @everyone", 15158332);
            City.BroadcastPropertyEvent(city, "Default color", "Details");
            City.BroadcastCityUpkeepPaid(city);
        };

        broadcast.Should().NotThrow("a failure to queue is logged, never thrown at the game");
    }

    [Test]
    public void The_upkeep_paid_notice_is_only_sent_once_nothing_is_owed()
    {
        var owing = StoreCity(upkeepOwed: 10);
        var paid = StoreCity();
        City.WebhookUrl = "https://example.invalid/hook";

        var broadcast = () =>
        {
            City.BroadcastCityUpkeepPaid(owing);
            City.BroadcastCityUpkeepPaid(paid);
        };

        broadcast.Should().NotThrow();
        paid.Dates.Should().ContainKey(PropertyDateType.Upkeep);
    }

    [Test]
    public void The_governance_uses_the_webhook_of_the_environment_unless_a_test_names_one()
    {
        var plain = new PropertyCityGovernance(Db, _world.Recorded);

        var city = StoreCity();
        var broadcast = () => plain.BroadcastPropertyEvent(city, "Title", "Details");

        broadcast.Should().NotThrow("the environment names no webhook in a test run, so nothing is posted");
    }
}
