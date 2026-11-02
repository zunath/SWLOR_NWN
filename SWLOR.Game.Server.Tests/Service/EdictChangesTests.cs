using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityTreasuryService;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Enacting, repealing, resuming and level pauses applied to in-memory city records, the way the Edict service applies
/// them before it saves.
/// </summary>
public class EdictChangesTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static WorldProperty City(string id, int level = 3, long treasury = 200000, double daysToCheck = 3.5)
    {
        var city = new WorldProperty
        {
            Id = id,
            CustomName = id,
            PropertyType = PropertyType.City,
            Treasury = treasury
        };
        city.Upgrades[PropertyUpgradeType.CityLevel] = level;
        city.Dates[PropertyDateType.Upkeep] = Now.AddDays(daysToCheck);
        return city;
    }

    private static EdictEnactment Enact(
        WorldProperty city,
        EdictType type,
        WorldProperty target = null,
        bool wildSpace = false,
        bool fromPlatform = false)
    {
        return EdictChanges.Enact(city, target, target?.Id ?? string.Empty, type, wildSpace, fromPlatform, Now);
    }

    #region Enacting

    [Test]
    public void Enacting_AddsAnActiveEdictWithASevenDayLock()
    {
        var city = City("home");

        var result = Enact(city, EdictType.FestivalSeason);

        result.Refusal.Should().Be(EdictRefusal.None);
        result.TakesEffect.Should().BeTrue();
        result.Charge.Should().Be(5000, "10,000 a week for the 3.5 days left");
        var edict = city.Edicts[EdictType.FestivalSeason];
        edict.State.Should().Be(EdictState.Active);
        edict.EnactedUtc.Should().Be(Now);
        edict.LockEndsUtc.Should().Be(Now.AddDays(7));
        edict.TargetCityId.Should().BeEmpty();
        edict.FromPlatform.Should().BeFalse();
        edict.IsPausedByCityLevel.Should().BeFalse();
    }

    [Test]
    public void Enacting_RecordsThatItCameFromThePlatform()
    {
        var city = City("home");

        Enact(city, EdictType.GuildCharter, fromPlatform: true);

        city.Edicts[EdictType.GuildCharter].FromPlatform.Should().BeTrue();
    }

    [Test]
    public void Enacting_ReturnsTheChargeWithoutTakingIt()
    {
        var city = City("home", treasury: 12345);

        var result = Enact(city, EdictType.FestivalSeason);

        result.Charge.Should().Be(5000);
        city.Treasury.Should().Be(12345, "the caller spends the charge through the treasury");
    }

    [Test]
    public void Enacting_IsRefusedWhenTheTreasuryCannotPayTheProration()
    {
        var city = City("home", treasury: 4999);

        var result = Enact(city, EdictType.FestivalSeason);

        result.Refusal.Should().Be(EdictRefusal.InsufficientFunds);
        city.Edicts.Should().BeEmpty();
    }

    [Test]
    public void Enacting_RestrictedFundsCountTowardTheProration()
    {
        var city = City("home", treasury: 1000);
        city.RestrictedTreasury = 4000;

        Enact(city, EdictType.FestivalSeason).Refusal.Should().Be(EdictRefusal.None);
    }

    [Test]
    public void Enacting_IsRefusedUnderACityWideCondition()
    {
        var city = City("home");
        city.NoConfidenceSinceUtc = Now.AddDays(-1);

        Enact(city, EdictType.FestivalSeason).Refusal.Should().Be(EdictRefusal.CityWideCondition);

        city.NoConfidenceSinceUtc = null;
        city.Upkeep = 35000;
        city.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(20);
        Enact(city, EdictType.FestivalSeason).Refusal.Should().Be(EdictRefusal.CityWideCondition);

        city.Dates.Remove(PropertyDateType.DisrepairDestruction);
        Enact(city, EdictType.FestivalSeason).Refusal.Should().Be(EdictRefusal.None, "a fresh bill is not disrepair");
    }

    [Test]
    public void Enacting_ARefusalChangesNothing()
    {
        var city = City("home", level: 1);

        Enact(city, EdictType.HuntingGrounds).Refusal.Should().Be(EdictRefusal.LevelTooLow);
        Enact(city, EdictType.ProspectorsCharter, wildSpace: true).Refusal.Should().Be(EdictRefusal.UnavailableHere);

        city.Edicts.Should().BeEmpty();
        city.Upkeep.Should().Be(0);
    }

    [Test]
    public void Planning_DoesNotChangeTheCity()
    {
        var city = City("home");

        var plan = EdictChanges.PlanEnact(city, null, string.Empty, EdictType.FestivalSeason, false, Now);

        plan.Refusal.Should().Be(EdictRefusal.None);
        plan.Charge.Should().Be(5000);
        city.Edicts.Should().BeEmpty();
    }

    [Test]
    public void Enacting_AnEmbargoNamesTheCityAndChargesItsOwnCost()
    {
        var city = City("home");
        var other = City("hutlar");

        var result = Enact(city, EdictType.Embargo, other);

        result.Charge.Should().Be(1250, "2,500 a week for 3.5 days");
        city.Edicts[EdictType.Embargo].TargetCityId.Should().Be("hutlar");
        city.Edicts[EdictType.Embargo].State.Should().Be(EdictState.Active);
    }

    [Test]
    public void Enacting_AnAllianceWithNoPartnerWaitsAndCostsNothing()
    {
        var city = City("home");
        var other = City("hutlar");

        var result = Enact(city, EdictType.Alliance, other);

        result.Refusal.Should().Be(EdictRefusal.None);
        result.TakesEffect.Should().BeFalse();
        result.Charge.Should().Be(0);
        result.ActivatedPartner.Should().BeFalse();
        var alliance = city.Edicts[EdictType.Alliance];
        alliance.State.Should().Be(EdictState.Pending);
        alliance.LockEndsUtc.Should().BeNull("the lock starts when both cities enact");
        alliance.TargetCityId.Should().Be("hutlar");
        other.Edicts.Should().BeEmpty();
    }

    [Test]
    public void Enacting_TheSecondAlliance_ActivatesBothAndStartsBothLocks()
    {
        var home = City("home");
        var hutlar = City("hutlar", daysToCheck: 2);
        Enact(hutlar, EdictType.Alliance, home);

        var result = Enact(home, EdictType.Alliance, hutlar);

        result.TakesEffect.Should().BeTrue();
        result.ActivatedPartner.Should().BeTrue();
        result.Charge.Should().Be(2500, "the enacting city pays 5,000 a week for 3.5 days");
        home.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Active);
        home.Edicts[EdictType.Alliance].LockEndsUtc.Should().Be(Now.AddDays(7));
        var waiting = hutlar.Edicts[EdictType.Alliance];
        waiting.State.Should().Be(EdictState.Active);
        waiting.LockEndsUtc.Should().Be(Now.AddDays(7));
    }

    [Test]
    public void Enacting_TheSecondAlliance_PutsTheWaitingCitysPartWeekOnItsUpkeepBill()
    {
        var home = City("home");
        var hutlar = City("hutlar", daysToCheck: 2);
        hutlar.Treasury = 0;
        Enact(hutlar, EdictType.Alliance, home);

        Enact(home, EdictType.Alliance, hutlar);

        hutlar.Upkeep.Should().Be(1428, "5,000 a week for the 2 days left before its own check");
        hutlar.Treasury.Should().Be(0, "the waiting city cannot be refused or charged from its treasury");
        home.Upkeep.Should().Be(0);
    }

    [Test]
    public void Enacting_TheSecondAlliance_DoesNotBillAWaitingCityThatIsInDisrepair()
    {
        var home = City("home");
        var hutlar = City("hutlar");
        Enact(hutlar, EdictType.Alliance, home);
        hutlar.Upkeep = 35000;
        hutlar.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(20);

        Enact(home, EdictType.Alliance, hutlar);

        hutlar.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Active);
        hutlar.Upkeep.Should().Be(35000, "a paused Edict resumes later, when the pause clears");
    }

    [Test]
    public void Enacting_AnAllianceDoesNotActivateAPartnerWaitingOnADifferentCity()
    {
        var home = City("home");
        var hutlar = City("hutlar");
        var veles = City("veles");
        Enact(hutlar, EdictType.Alliance, veles);

        var result = Enact(home, EdictType.Alliance, hutlar);

        result.TakesEffect.Should().BeFalse();
        result.ActivatedPartner.Should().BeFalse();
        hutlar.Edicts[EdictType.Alliance].State.Should().Be(EdictState.Pending);
    }

    [Test]
    public void Enacting_AnAllianceAgainstAnEmbargoedCityIsRefused()
    {
        var home = City("home", level: 5);
        var hutlar = City("hutlar");
        Enact(home, EdictType.Embargo, hutlar);

        Enact(home, EdictType.Alliance, hutlar).Refusal.Should().Be(EdictRefusal.ExclusivePair);
    }

    #endregion

    #region Repealing

    private static WorldProperty CityWith(string id, EdictType type, int level = 3, bool fromPlatform = false)
    {
        var city = City(id, level);
        Enact(city, type, fromPlatform: fromPlatform);
        return city;
    }

    [Test]
    public void Repeal_IsRefusedDuringTheLock()
    {
        var city = CityWith("home", EdictType.FestivalSeason);

        var result = EdictChanges.Repeal(city, null, EdictType.FestivalSeason, Now.AddDays(6));

        result.Refusal.Should().Be(EdictRefusal.StillLocked);
        city.Edicts.Should().ContainKey(EdictType.FestivalSeason);
    }

    [Test]
    public void Repeal_RemovesTheEdictAfterTheLockWithNoRefund()
    {
        var city = CityWith("home", EdictType.FestivalSeason);
        var treasury = city.Treasury;

        var result = EdictChanges.Repeal(city, null, EdictType.FestivalSeason, Now.AddDays(7));

        result.Refusal.Should().Be(EdictRefusal.None);
        city.Edicts.Should().BeEmpty();
        city.Treasury.Should().Be(treasury);
        city.Upkeep.Should().Be(0);
    }

    [Test]
    public void Repeal_OfAnEdictThatIsNotHeldIsRefused()
    {
        EdictChanges.Repeal(City("home"), null, EdictType.GuildCharter, Now).Refusal.Should().Be(EdictRefusal.NotHeld);
    }

    [Test]
    public void Repeal_OfAPlatformEdictMarksABrokenPromise()
    {
        var city = CityWith("home", EdictType.GuildCharter, fromPlatform: true);
        city.MayorRecord.Promised = 2;

        var result = EdictChanges.Repeal(city, null, EdictType.GuildCharter, Now.AddDays(8));

        result.BrokenPromise.Should().BeTrue();
        city.MayorRecord.Broken.Should().Be(1);
        city.MayorRecord.Promised.Should().Be(2);
    }

    [Test]
    public void Repeal_OfAnEdictEnactedMidTermIsNotABrokenPromise()
    {
        var city = CityWith("home", EdictType.GuildCharter);

        var result = EdictChanges.Repeal(city, null, EdictType.GuildCharter, Now.AddDays(8));

        result.BrokenPromise.Should().BeFalse();
        city.MayorRecord.Broken.Should().Be(0);
    }

    [Test]
    public void Repeal_OfAnEdictPausedByTheLevelStillBreaksAPlatformPromise()
    {
        var city = CityWith("home", EdictType.GuildCharter, fromPlatform: true);
        city.Edicts[EdictType.GuildCharter].IsPausedByCityLevel = true;

        EdictChanges.Repeal(city, null, EdictType.GuildCharter, Now.AddDays(8)).BrokenPromise.Should().BeTrue();
    }

    [Test]
    public void Repeal_OfAnActiveAllianceEndsThePartnersToo()
    {
        var home = City("home");
        var hutlar = City("hutlar");
        Enact(hutlar, EdictType.Alliance, home);
        Enact(home, EdictType.Alliance, hutlar);

        var result = EdictChanges.Repeal(home, hutlar, EdictType.Alliance, Now.AddDays(8));

        result.EndedPartnerAlliance.Should().BeTrue();
        home.Edicts.Should().BeEmpty();
        hutlar.Edicts.Should().BeEmpty();
    }

    [Test]
    public void Repeal_OfAPendingAllianceLeavesTheOtherCityAlone()
    {
        var home = City("home");
        var hutlar = City("hutlar");
        var veles = City("veles");
        Enact(home, EdictType.Alliance, hutlar);
        Enact(hutlar, EdictType.Alliance, veles);

        var result = EdictChanges.Repeal(home, hutlar, EdictType.Alliance, Now);

        result.Refusal.Should().Be(EdictRefusal.None, "a pending Alliance has no lock");
        result.EndedPartnerAlliance.Should().BeFalse();
        hutlar.Edicts.Should().ContainKey(EdictType.Alliance);
    }

    [Test]
    public void Repeal_OfAnAllianceDoesNotEndAPartnerAllianceNamingAnotherCity()
    {
        var home = City("home");
        var hutlar = City("hutlar");
        var veles = City("veles");
        Enact(hutlar, EdictType.Alliance, home);
        Enact(home, EdictType.Alliance, hutlar);
        hutlar.Edicts[EdictType.Alliance].TargetCityId = veles.Id;

        EdictChanges.Repeal(home, hutlar, EdictType.Alliance, Now.AddDays(8)).EndedPartnerAlliance.Should().BeFalse();
        hutlar.Edicts.Should().ContainKey(EdictType.Alliance);
    }

    [Test]
    public void Repeal_AFreedSlotResumesAnEdictTheLevelPausedAndBillsItsPartWeek()
    {
        // A level 4 city held two Edicts; at level 2 the newer one is paused for the slot count.
        var city = City("home", level: 4);
        EdictChanges.Enact(city, null, string.Empty, EdictType.GuildCharter, false, false, Now.AddDays(-20));
        EdictChanges.Enact(city, null, string.Empty, EdictType.DrydockSubsidy, false, false, Now.AddDays(-10));
        city.Upgrades[PropertyUpgradeType.CityLevel] = 2;
        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: false);
        city.Edicts[EdictType.DrydockSubsidy].IsPausedByCityLevel.Should().BeTrue();

        EdictChanges.Repeal(city, null, EdictType.GuildCharter, Now);

        city.Edicts[EdictType.DrydockSubsidy].IsPausedByCityLevel.Should().BeFalse();
        city.Upkeep.Should().Be(5000, "10,000 a week for the 3.5 days left");
    }

    #endregion

    #region Level pauses

    [Test]
    public void ALevelDrop_PausesEdictsAndTheLevelReturningClearsThem()
    {
        var city = City("home", level: 5);
        Enact(city, EdictType.HuntingGrounds);
        city.Upgrades[PropertyUpgradeType.CityLevel] = 2;

        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: false);
        city.Edicts[EdictType.HuntingGrounds].IsPausedByCityLevel.Should().BeTrue();
        EdictChanges.GetBilledTypes(city).Should().BeEmpty();

        city.Upgrades[PropertyUpgradeType.CityLevel] = 3;
        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: false);
        city.Edicts[EdictType.HuntingGrounds].IsPausedByCityLevel.Should().BeFalse();
        EdictChanges.GetBilledTypes(city).Should().BeEquivalentTo(new[] { EdictType.HuntingGrounds });
    }

    [Test]
    public void TheWeeklyCheck_BillsAnEdictThatResumesAtTheCheckItselfSoItAddsNoPartWeek()
    {
        var city = City("home", level: 5);
        Enact(city, EdictType.HuntingGrounds);
        city.Edicts[EdictType.HuntingGrounds].IsPausedByCityLevel = true;

        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: false);

        city.Upkeep.Should().Be(0);
    }

    [Test]
    public void BetweenChecks_ALevelResumeAddsThePartWeek()
    {
        var city = City("home", level: 5);
        Enact(city, EdictType.HuntingGrounds);
        city.Edicts[EdictType.HuntingGrounds].IsPausedByCityLevel = true;

        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: true);

        city.Upkeep.Should().Be(7500, "15,000 a week for the 3.5 days left");
    }

    [Test]
    public void APendingAlliance_IsNeverBilledByTheLevelCheck()
    {
        var city = City("home", level: 1);
        city.Edicts[EdictType.Alliance] = new CityEdict { State = EdictState.Pending, EnactedUtc = Now, TargetCityId = "hutlar" };

        EdictChanges.ApplyCityLevelPauses(city, Now, billResumes: true);

        city.Upkeep.Should().Be(0);
        EdictChanges.GetBilledTypes(city).Should().BeEmpty();
    }

    #endregion

    #region Resuming between checks

    [Test]
    public void PayingUpkeepOutOfDisrepair_ResumesEachEdictAndAddsItsPartWeek()
    {
        var city = City("home", level: 5, treasury: 500000);
        Enact(city, EdictType.FestivalSeason);
        Enact(city, EdictType.GuildCharter);
        city.Upkeep = 80000;
        city.Dates[PropertyDateType.DisrepairDestruction] = Now.AddDays(20);
        var billedBefore = EdictChanges.GetBilledTypes(city);
        billedBefore.Should().BeEmpty("disrepair pauses every Edict");

        CityTreasuryRules.PayUpkeep(city).Should().BeTrue();
        var added = EdictChanges.AddResumedBilling(city, billedBefore, Now);

        added.Should().Be(5000 + 7500);
        city.Upkeep.Should().Be(12500);
        EdictChanges.GetConditions(city).Should().Be(CityWideCondition.None, "the new part-week is a fresh bill, not disrepair");
    }

    [Test]
    public void ResumeBilling_DoesNotBillAnEdictThatWasAlreadyBilled()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);

        EdictChanges.AddResumedBilling(city, EdictChanges.GetBilledTypes(city), Now).Should().Be(0);
        city.Upkeep.Should().Be(0);
    }

    [Test]
    public void ResumeBilling_AnEdictPausedBySituationWasNeverUnbilledSoItAddsNothing()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);
        city.Edicts[EdictType.FestivalSeason].SituationPauseEndsUtc = Now.AddHours(48);

        EdictChanges.AddResumedBilling(city, EdictChanges.GetBilledTypes(city), Now).Should().Be(0);
        EdictChanges.GetBilledTypes(city).Should().BeEquivalentTo(new[] { EdictType.FestivalSeason });
    }

    [Test]
    public void ResumeBilling_IsNothingWhenTheNextCheckIsOverdue()
    {
        var city = City("home", daysToCheck: -1);
        Enact(city, EdictType.FestivalSeason);
        city.NoConfidenceSinceUtc = Now.AddDays(-9);
        var billedBefore = EdictChanges.GetBilledTypes(city);
        city.NoConfidenceSinceUtc = null;

        EdictChanges.AddResumedBilling(city, billedBefore, Now).Should().Be(0, "the weekly check bills the Edict in full");
    }

    #endregion

    #region Asking

    [Test]
    public void AnEdict_IsInEffectOnlyWhenNothingPausesIt()
    {
        var city = City("home");
        Enact(city, EdictType.FestivalSeason);

        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().NotBeNull();
        EdictChanges.GetInEffect(city, EdictType.GuildCharter, Now).Should().BeNull();
        EdictChanges.GetInEffect(null, EdictType.FestivalSeason, Now).Should().BeNull();

        city.NeglectSinceUtc = Now;
        EdictChanges.GetInEffect(city, EdictType.FestivalSeason, Now).Should().BeNull();
    }

    [Test]
    public void RemovingAnAllianceNamingACity_FreesTheSlotAndLeavesOtherEdicts()
    {
        var home = City("home");
        Enact(home, EdictType.Alliance, City("hutlar"));
        Enact(home, EdictType.FestivalSeason);

        EdictChanges.RemoveAllianceNaming(home, "hutlar", Now).Should().BeTrue();
        EdictChanges.RemoveAllianceNaming(home, "hutlar", Now).Should().BeFalse();

        home.Edicts.Keys.Should().Equal(EdictType.FestivalSeason);
    }

    [Test]
    public void RemovingAnAllianceNamingACity_IgnoresAnAllianceNamingAnotherCity()
    {
        var home = City("home");
        Enact(home, EdictType.Alliance, City("hutlar"));

        EdictChanges.RemoveAllianceNaming(home, "veles", Now).Should().BeFalse();
        home.Edicts.Should().ContainKey(EdictType.Alliance);
    }

    #endregion
}
