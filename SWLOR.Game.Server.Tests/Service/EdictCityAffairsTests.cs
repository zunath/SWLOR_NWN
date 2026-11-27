using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.SituationDefinition;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// How the Edicts and City Affairs meet: situations read which Edicts are in effect, a situation can suspend Open Gates,
/// and a new mayor starts with a clean record.
/// </summary>
public class EdictCityAffairsTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static WorldProperty CityWith(params EdictType[] types)
    {
        var city = new WorldProperty { Id = "home", PropertyType = PropertyType.City, Treasury = 500000 };
        city.Upgrades[PropertyUpgradeType.CityLevel] = 5;
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(3.5);
        foreach (var type in types)
            EdictChanges.Enact(city, null, string.Empty, type, false, false, Now.AddDays(-10));

        return city;
    }

    private static IReadOnlyCollection<string> ActiveNames(WorldProperty city)
    {
        return EdictChanges.GetInEffectTypes(city, Now).Select(type => type.ToString()).ToList();
    }

    [Test]
    public void SituationsNameEdictsByTheirEdictTypeMembers()
    {
        CitizenSituationDefinition.OpenGatesEdict.Should().Be(nameof(EdictType.OpenGates));
    }

    [Test]
    public void TheActiveEdictNames_AreTheEdictsInEffectInTypeOrder()
    {
        var city = CityWith(EdictType.OpenGates, EdictType.FestivalSeason, EdictType.GuildCharter);

        ActiveNames(city).Should().Equal("FestivalSeason", "OpenGates", "GuildCharter");
    }

    [Test]
    public void ACityWithNoEdicts_HasNoActiveEdictNames()
    {
        ActiveNames(CityWith()).Should().BeEmpty();
    }

    [Test]
    public void AnEdictThatIsPausedOrPending_IsNotAnActiveName()
    {
        var city = CityWith(EdictType.OpenGates);
        city.Edicts[EdictType.Alliance] = new CityEdict { State = EdictState.Pending, TargetCityId = "hutlar", EnactedUtc = Now };

        city.Edicts[EdictType.OpenGates].IsPausedByCityLevel = true;
        ActiveNames(city).Should().BeEmpty("a level pause and a pending Alliance do not count");

        city.Edicts[EdictType.OpenGates].IsPausedByCityLevel = false;
        ActiveNames(city).Should().Equal("OpenGates");

        city.Upkeep = 35000;
        city.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(20);
        ActiveNames(city).Should().BeEmpty("disrepair pauses every Edict");
    }

    [Test]
    public void VisitorBrawl_NeedsAnOpenGatesEdictThatIsInEffect()
    {
        var brawl = SituationCatalog.Get(SituationType.VisitorBrawl);

        bool Met(WorldProperty city) => SituationRules.DescribeUnmet(
            brawl.Requirements,
            new SituationContext { CityLevel = 5, ActiveEdicts = ActiveNames(city) }).Count == 0;

        Met(CityWith(EdictType.OpenGates)).Should().BeTrue();
        Met(CityWith(EdictType.FestivalSeason)).Should().BeFalse("it needs Open Gates, not any Edict");
        Met(CityWith()).Should().BeFalse();

        var noConfidence = CityWith(EdictType.OpenGates);
        noConfidence.NoConfidenceSinceUtc = Now.AddDays(-1);
        Met(noConfidence).Should().BeFalse("the Edict is paused");
    }

    [Test]
    public void ClosingTheGates_PausesOpenGatesButItIsStillBilled()
    {
        var city = CityWith(EdictType.OpenGates, EdictType.FestivalSeason);
        city.Modifiers[CityModifierType.OpenGatesSuspended] = new CityModifier
        {
            ExpiresUtc = Now.AddHours(48),
            Source = SituationType.VisitorBrawl
        };

        EdictChanges.GetInEffect(city, EdictType.OpenGates, Now).Should().BeNull();
        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().NotBeNull("only Open Gates is suspended");
        EdictChanges.GetBilledTypes(city).Should().Contain(EdictType.OpenGates, "a situation pause is still billed");
        EdictRules.GetBillLines(city.Edicts, EdictChanges.GetConditions(city)).Select(x => x.Type)
            .Should().Contain(EdictType.OpenGates);
    }

    [Test]
    public void TheGatesReopen_WhenTheModifierExpires()
    {
        var city = CityWith(EdictType.OpenGates);
        city.Modifiers[CityModifierType.OpenGatesSuspended] = new CityModifier { ExpiresUtc = Now.AddHours(1) };

        EdictChanges.GetInEffect(city, EdictType.OpenGates, Now).Should().BeNull();
        EdictChanges.GetInEffect(city, EdictType.OpenGates, Now.AddHours(1)).Should().NotBeNull();
    }

    [Test]
    public void AnotherModifier_DoesNotPauseAnyEdict()
    {
        var city = CityWith(EdictType.OpenGates, EdictType.FestivalSeason);
        city.Modifiers[CityModifierType.FestivalXpBonus] = new CityModifier { ExpiresUtc = Now.AddHours(48) };

        EdictChanges.GetInEffectTypes(city, Now).Should().Equal(EdictType.FestivalSeason, EdictType.OpenGates);
    }

    [Test]
    public void TheEdictsTab_SaysWhenASituationHasSuspendedAnEdict()
    {
        var city = CityWith(EdictType.OpenGates);
        city.Modifiers[CityModifierType.OpenGatesSuspended] = new CityModifier { ExpiresUtc = Now.AddHours(48) };
        var edict = city.Edicts[EdictType.OpenGates];

        EdictRules.BuildHeldText(
                EdictType.OpenGates,
                edict,
                null,
                EdictChanges.GetConditions(city),
                Now,
                EdictChanges.IsPausedBySituation(city, EdictType.OpenGates, edict, Now))
            .Should().Contain("Paused: a situation outcome");
    }

    [Test]
    public void ANewMayor_StartsWithACleanRecord()
    {
        var property = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Service", "Property.cs"));
        var transfer = property.Substring(
            property.IndexOf("void TransferPermissions(string winnerPlayerId)", StringComparison.Ordinal),
            property.IndexOf("// No reason to process deleted cities", StringComparison.Ordinal) -
            property.IndexOf("void TransferPermissions(string winnerPlayerId)", StringComparison.Ordinal));

        var officers = transfer.IndexOf("CityOfficers.EndAllOnNewMayor(city, winnerPlayerId);", StringComparison.Ordinal);
        var record = transfer.IndexOf("city.MayorRecord = new MayorRecord();", StringComparison.Ordinal);
        officers.Should().BeGreaterThanOrEqualTo(0);
        record.Should().BeGreaterThan(officers, "the record resets where the old mayor's officers and permissions end");
    }

    [Test]
    public void ACityAffairsContext_ReadsEdictsFromTheEdictService()
    {
        var affairs = File.ReadAllText(Path.Combine(RepositoryRoot, "SWLOR.Game.Server", "Service", "CityAffairs.cs"));

        affairs.Should().Contain("EdictChanges.GetInEffectTypes(city, DateTime.UtcNow)");
        affairs.Should().NotContain("Array.Empty<string>()");
    }

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
