using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// An outright winner's platform replacing the city's Edicts when the results post (Player City Revamp, "Edict rules"
/// items 3 and 5): the replacement over locks, the first part-week, the mayor's record, the city-wide conditions that
/// remain, the level pauses, and Alliances, applied to in-memory records.
/// </summary>
public class EdictPlatformTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static WorldProperty City(string id, int level = 3, double daysToCheck = 3.5)
    {
        var city = new WorldProperty
        {
            Id = id,
            CustomName = id,
            PropertyType = PropertyType.City,
            Treasury = 100000
        };
        city.Upgrades[PropertyUpgradeType.CityLevel] = level;
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(daysToCheck);
        return city;
    }

    private static List<ElectionPlatformEdict> Platform(params EdictType[] types)
    {
        return types.Select(x => new ElectionPlatformEdict { Type = x }).ToList();
    }

    private static ElectionPlatformEdict Naming(EdictType type, WorldProperty target)
    {
        return new ElectionPlatformEdict { Type = type, TargetCityId = target.Id };
    }

    private static EdictPlatformEnactment EnactPlatform(
        WorldProperty city,
        IEnumerable<ElectionPlatformEdict> platform,
        bool wildSpace = false,
        params WorldProperty[] others)
    {
        var known = others.ToDictionary(x => x.Id);
        return EdictChanges.EnactPlatform(
            city,
            platform,
            id => known.TryGetValue(id, out var other) ? other : null,
            wildSpace,
            Now);
    }

    private static void Enact(WorldProperty city, EdictType type, WorldProperty target = null)
    {
        EdictChanges.Enact(city, target, target?.Id ?? string.Empty, type, false, false, Now).Refusal
            .Should().Be(EdictRefusal.None);
    }

    #region Replacing Edicts

    [Test]
    public void Platform_ReplacesEveryEdictWhateverTheLocks()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);
        Enact(city, EdictType.GuildCharter);
        city.Edicts[EdictType.FestivalSeason].LockEndsUtc.Should().BeAfter(Now, "the old Edicts are locked");

        var result = EnactPlatform(city, Platform(EdictType.OpenGates));

        city.Edicts.Keys.Should().BeEquivalentTo(new[] { EdictType.OpenGates });
        result.ReplacedCount.Should().Be(2);
        result.Enacted.Select(x => x.Type).Should().Equal(EdictType.OpenGates);
    }

    [Test]
    public void Platform_EdictsAreActiveFromThePlatformWithAFreshSevenDayLock()
    {
        var city = City("home");

        EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter));

        foreach (var edict in city.Edicts.Values)
        {
            edict.State.Should().Be(EdictState.Active);
            edict.FromPlatform.Should().BeTrue();
            edict.EnactedUtc.Should().Be(Now);
            edict.LockEndsUtc.Should().Be(Now.AddDays(7));
            edict.IsPausedByCityLevel.Should().BeFalse();
        }
    }

    [Test]
    public void Platform_WithNoEdicts_EndsTheCitysEdictsAndPromisesNothing()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);

        var result = EnactPlatform(city, Platform());

        city.Edicts.Should().BeEmpty();
        result.Enacted.Should().BeEmpty();
        result.ReplacedCount.Should().Be(1);
        city.MayorRecord.Promised.Should().Be(0);
    }

    [Test]
    public void Platform_ListedOutOfOrderAndTwice_IsEnactedOncePerEdict()
    {
        var city = City("home");

        var result = EnactPlatform(city, Platform(EdictType.GuildCharter, EdictType.FestivalSeason, EdictType.GuildCharter));

        result.Enacted.Select(x => x.Type).Should().Equal(EdictType.FestivalSeason, EdictType.GuildCharter);
        result.Dropped.Should().Equal((EdictType.GuildCharter, EdictRefusal.AlreadyHeld));
        city.Edicts.Should().HaveCount(2);
    }

    [Test]
    public void Platform_DoesNotChargeTheTreasury()
    {
        var city = City("home");
        city.RestrictedTreasury = 777;

        EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter));

        city.Treasury.Should().Be(100000);
        city.RestrictedTreasury.Should().Be(777);
    }

    #endregion

    #region Billing

    [Test]
    public void Platform_AddsTheFirstPartWeekOfEachEdictToTheUpkeepBill()
    {
        var city = City("home");

        var result = EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter));

        result.PartWeekAdded.Should().Be(12500, "10,000 and 15,000 a week for the 3.5 days left");
        city.Upkeep.Should().Be(12500);
    }

    [Test]
    public void Platform_AddsNoPartWeekWhenTheWeeklyCheckIsOverdue()
    {
        var city = City("home", daysToCheck: -1);

        var result = EnactPlatform(city, Platform(EdictType.FestivalSeason));

        result.PartWeekAdded.Should().Be(0, "the check that runs next bills the full week");
        city.Upkeep.Should().Be(0);
    }

    [Test]
    public void Platform_BillsAnEdictAgainEvenWhenTheCityHeldItBefore()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);

        EnactPlatform(city, Platform(EdictType.FestivalSeason));

        city.Upkeep.Should().Be(5000, "replacing an Edict enacts it again, and nothing is refunded");
    }

    [Test]
    public void Platform_AfterTheElectionEndedNoConfidence_NewEdictsWorkAndPayTheirPartWeek()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);
        city.NoConfidenceSinceUtc = Now.AddDays(-5);
        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().BeNull("No confidence paused it");

        // The election's outright win clears the pause before the platform is enacted.
        city.NoConfidenceSinceUtc = null;
        EnactPlatform(city, Platform(EdictType.GuildCharter));

        EdictChanges.GetInEffect(city, EdictType.GuildCharter, Now).Should().NotBeNull();
        city.Upkeep.Should().Be(7500);
    }

    [Test]
    public void Platform_AfterTheElectionEndedNeglect_NewEdictsWorkAndPayTheirPartWeek()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);
        city.Upkeep = 0;
        city.NeglectSinceUtc = Now.AddDays(-20);
        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().BeNull("Neglect paused it");

        // An election with a candidate and a vote clears Neglect before the winner's platform is enacted.
        city.NeglectSinceUtc = null;
        EnactPlatform(city, Platform(EdictType.GuildCharter));

        EdictChanges.GetInEffect(city, EdictType.GuildCharter, Now).Should().NotBeNull();
        city.Upkeep.Should().Be(7500);
    }

    [Test]
    public void Platform_UnderNoConfidenceThatRemains_KeepsTheNewEdictsPausedAndUnbilled()
    {
        var city = City("home");
        city.NoConfidenceSinceUtc = Now.AddDays(-5);

        EnactPlatform(city, Platform(EdictType.FestivalSeason));

        city.Edicts.Should().ContainKey(EdictType.FestivalSeason);
        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().BeNull();
        city.Upkeep.Should().Be(0);
    }

    [Test]
    public void Platform_UnderADisrepairThatRemains_PausesTheNewEdictsAndTheyPayTheirPartWeekWhenTheBillIsPaid()
    {
        var city = City("home");
        city.Upkeep = 35000;
        city.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(20);

        EnactPlatform(city, Platform(EdictType.FestivalSeason));

        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().BeNull("the bill is still unpaid");
        city.Upkeep.Should().Be(35000, "nothing is added while the Edict is paused");

        // Paying the bill, as the treasury does, lifts the pause and bills what resumes.
        var billedBefore = EdictChanges.GetBilledTypes(city);
        city.Upkeep = 0;
        city.Dates.Remove(PropertyDateType.DisrepairDestruction);
        EdictChanges.AddResumedBilling(city, billedBefore, Now).Should().Be(5000);
        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().NotBeNull();
    }

    #endregion

    #region Mayor's record

    [Test]
    public void Platform_SetsThePromisedCountAndStartsTheRecordAgain()
    {
        var city = City("home");
        city.MayorRecord.Promised = 3;
        city.MayorRecord.Broken = 2;

        EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter));

        city.MayorRecord.Promised.Should().Be(2);
        city.MayorRecord.Broken.Should().Be(0);
    }

    [Test]
    public void Platform_ThenRepealingOneOfItsEdictsBreaksAPromise()
    {
        var city = City("home");
        EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter));

        var repeal = EdictChanges.Repeal(city, null, EdictType.GuildCharter, Now.AddDays(8));

        repeal.BrokenPromise.Should().BeTrue();
        city.MayorRecord.Promised.Should().Be(2);
        city.MayorRecord.Broken.Should().Be(1);
        ElectionPlatformRules.BuildRecordText(city.MayorRecord).Should().Be("Promises kept: 1 of 2");
    }

    [Test]
    public void Platform_AnEdictThePlatformDropsIsNotPromised()
    {
        var city = City("home");
        var gone = new WorldProperty { Id = "gone" };

        var result = EnactPlatform(city, new[] { new ElectionPlatformEdict { Type = EdictType.Alliance, TargetCityId = gone.Id } });

        result.Enacted.Should().BeEmpty();
        city.MayorRecord.Promised.Should().Be(0);
    }

    [Test]
    public void ClearPlatformOrigin_LeavesTheEdictsButNoLongerCountsThemAsPromises()
    {
        var city = City("home");
        EnactPlatform(city, Platform(EdictType.FestivalSeason));

        EdictChanges.ClearPlatformOrigin(city);

        city.Edicts[EdictType.FestivalSeason].FromPlatform.Should().BeFalse();
        city.Edicts.Should().ContainKey(EdictType.FestivalSeason);
        city.MayorRecord.Broken = 0;
        EdictChanges.Repeal(city, null, EdictType.FestivalSeason, Now.AddDays(8)).BrokenPromise.Should().BeFalse();
    }

    #endregion

    #region City level

    [Test]
    public void Platform_ThatTheLevelCannotHold_IsEnactedInFullThenPausedWithoutBilling()
    {
        var city = City("home", level: 1);

        var result = EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter, EdictType.HuntingGrounds));

        city.Edicts.Should().HaveCount(3, "the platform is enacted in full");
        city.Edicts[EdictType.HuntingGrounds].IsPausedByCityLevel.Should().BeTrue("its minimum level is 3");
        city.Edicts[EdictType.GuildCharter].IsPausedByCityLevel.Should().BeTrue("the newest above the one slot pause next");
        city.Edicts[EdictType.FestivalSeason].IsPausedByCityLevel.Should().BeFalse();
        result.PartWeekAdded.Should().Be(5000, "only the Edict that works pays");
        city.MayorRecord.Promised.Should().Be(3, "a paused Edict is still promised");
    }

    [Test]
    public void Platform_PausedByTheLevel_ResumesAndPaysItsPartWeekWhenTheLevelReturns()
    {
        var city = City("home", level: 1);
        EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.GuildCharter));
        var upkeep = city.Upkeep;

        city.Upgrades[PropertyUpgradeType.CityLevel] = 3;
        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: true);

        city.Edicts[EdictType.GuildCharter].IsPausedByCityLevel.Should().BeFalse();
        city.Upkeep.Should().Be(upkeep + 7500);
        city.MayorRecord.Broken.Should().Be(0, "a pause is not a broken promise");
    }

    #endregion

    #region Wild Space and missing cities

    [Test]
    public void Platform_DropsPlanetEdictsOnAWildSpaceStation()
    {
        var city = City("station");

        var result = EnactPlatform(city, Platform(EdictType.FestivalSeason, EdictType.ProspectorsCharter), wildSpace: true);

        result.Dropped.Should().Equal((EdictType.ProspectorsCharter, EdictRefusal.UnavailableHere));
        city.Edicts.Keys.Should().BeEquivalentTo(new[] { EdictType.FestivalSeason });
        city.MayorRecord.Promised.Should().Be(1);
    }

    [Test]
    public void Platform_DropsAnAllianceWhoseCityIsGoneOrNotNamed()
    {
        var city = City("home");

        var result = EnactPlatform(city, new[]
        {
            new ElectionPlatformEdict { Type = EdictType.Alliance, TargetCityId = "gone" },
            new ElectionPlatformEdict { Type = EdictType.Embargo }
        });

        result.Dropped.Select(x => x.Refusal).Should().Equal(EdictRefusal.InvalidTarget, EdictRefusal.TargetRequired);
        city.Edicts.Should().BeEmpty();
    }

    #endregion

    #region Alliances

    [Test]
    public void Platform_Alliance_WaitsWithoutCostWhenTheOtherMayorHasNotEnacted()
    {
        var city = City("home");
        var hutlar = City("hutlar");

        var result = EnactPlatform(city, new[] { Naming(EdictType.Alliance, hutlar) }, false, hutlar);

        var alliance = city.Edicts[EdictType.Alliance];
        alliance.State.Should().Be(EdictState.Pending);
        alliance.LockEndsUtc.Should().BeNull();
        alliance.TargetCityId.Should().Be("hutlar");
        alliance.FromPlatform.Should().BeTrue();
        result.PartWeekAdded.Should().Be(0);
        result.FormedAlliances.Should().BeEmpty();
        city.MayorRecord.Promised.Should().Be(1);
    }

    [Test]
    public void Platform_Alliance_TakesEffectWithAWaitingPartnerAndBillsBoth()
    {
        var city = City("home");
        var hutlar = City("hutlar");
        Enact(hutlar, EdictType.Alliance, city);

        var result = EnactPlatform(city, new[] { Naming(EdictType.Alliance, hutlar) }, false, hutlar);

        city.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Active);
        city.Edicts[EdictType.Alliance].LockEndsUtc.Should().Be(Now.AddDays(7));
        hutlar.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Active);
        hutlar.Edicts[EdictType.Alliance].LockEndsUtc.Should().Be(Now.AddDays(7));
        result.FormedAlliances.Should().Equal(hutlar);
        result.ChangedCities.Should().Equal(hutlar);
        city.Upkeep.Should().Be(2500, "5,000 a week for the 3.5 days left");
        hutlar.Upkeep.Should().Be(2500, "the waiting city's part-week joins its bill");
    }

    [Test]
    public void Platform_ReplacingAnActiveAlliance_EndsItForTheOtherCity()
    {
        var city = City("home");
        var hutlar = City("hutlar");
        Enact(hutlar, EdictType.Alliance, city);
        Enact(city, EdictType.Alliance, hutlar);

        var result = EnactPlatform(city, Platform(EdictType.FestivalSeason), false, hutlar);

        hutlar.Edicts.Should().NotContainKey(EdictType.Alliance);
        result.EndedAlliances.Should().Equal(hutlar);
        result.ChangedCities.Should().Equal(hutlar);
    }

    [Test]
    public void Platform_EnactingTheSameAllianceAgain_KeepsThePartnershipGoing()
    {
        var city = City("home");
        var hutlar = City("hutlar");
        Enact(hutlar, EdictType.Alliance, city);
        Enact(city, EdictType.Alliance, hutlar);
        var partnerLock = hutlar.Edicts[EdictType.Alliance].LockEndsUtc;

        var result = EnactPlatform(city, new[] { Naming(EdictType.Alliance, hutlar) }, false, hutlar);

        hutlar.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Active);
        hutlar.Edicts[EdictType.Alliance].LockEndsUtc.Should().Be(partnerLock, "the other city's Alliance is untouched");
        city.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Active);
        city.Edicts[EdictType.Alliance].LockEndsUtc.Should().Be(Now.AddDays(7));
        result.EndedAlliances.Should().BeEmpty();
        result.FormedAlliances.Should().BeEmpty();
    }

    [Test]
    public void Platform_ReplacingAPendingAlliance_LeavesTheOtherCityAlone()
    {
        var city = City("home");
        var hutlar = City("hutlar");
        Enact(city, EdictType.Alliance, hutlar);

        var result = EnactPlatform(city, Platform(EdictType.FestivalSeason), false, hutlar);

        result.EndedAlliances.Should().BeEmpty();
        result.ChangedCities.Should().BeEmpty();
    }

    [Test]
    public void Platform_AnAllianceAndAnEmbargoAgainstTheSameCity_KeepsOnlyTheFirst()
    {
        var city = City("home");
        var hutlar = City("hutlar");

        var result = EnactPlatform(city, new[] { Naming(EdictType.Alliance, hutlar), Naming(EdictType.Embargo, hutlar) }, false, hutlar);

        city.Edicts.Keys.Should().BeEquivalentTo(new[] { EdictType.Alliance });
        result.Dropped.Should().Equal((EdictType.Embargo, EdictRefusal.ExclusivePair));
    }

    #endregion

    #region Gazette text

    [Test]
    public void GazetteText_NamesTheEdictsAndSaysWhetherEarlierOnesEnded()
    {
        EdictRules.BuildPlatformEnactedText(new[] { (EdictType.FestivalSeason, string.Empty), (EdictType.Embargo, "Hutlar") }, 1)
            .Should().Be("The mayor's election platform took effect. Edicts enacted: Festival Season, Embargo naming Hutlar. The city's earlier Edicts ended.");
        EdictRules.BuildPlatformEnactedText(new[] { (EdictType.FestivalSeason, string.Empty) }, 0)
            .Should().Be("The mayor's election platform took effect. Edicts enacted: Festival Season.");
        EdictRules.BuildPlatformEnactedText(Array.Empty<(EdictType, string)>(), 2)
            .Should().Be("The mayor's election platform took effect with no Edicts. The city's earlier Edicts ended.");
    }

    #endregion
}
