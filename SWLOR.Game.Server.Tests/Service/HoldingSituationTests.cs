using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CityAffairsService;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.StationService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Holding events as City Affairs situations (Player City Revamp, "Planetary industry"): a raid or a customs fine at a citizen's
/// holding on the city's planet can raise a situation, whose choices send help or pay the fine from the treasury.
/// </summary>
public class HoldingSituationTests
{
    private static readonly DateTime Now = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private static SituationDetail Raid => SituationCatalog.Get(SituationType.HoldingRaid);
    private static SituationDetail Customs => SituationCatalog.Get(SituationType.HoldingCustoms);

    private static SituationChoice Choice(SituationDetail detail, string text) => detail.Choices.Single(choice => choice.Text == text);

    private static PlanetaryHolding Holding(HoldingType type = HoldingType.SpiceLab) =>
        HoldingRules.Create("owner-player-id", HoldingRules.GetDetail(type), "dock", Now);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException();
        }
    }

    private static string Source(params string[] relative) =>
        File.ReadAllText(Path.Combine(new[] { Root, "SWLOR.Game.Server" }.Concat(relative).ToArray()));

    // The definitions

    [Test]
    public void TheTwoSituationsFollowTheExistingOnes_InValueAndShape()
    {
        ((int)SituationType.HoldingRaid).Should().Be(15);
        ((int)SituationType.HoldingCustoms).Should().Be(16);
        Enum.GetValues<SituationType>().Cast<int>().Max().Should().BeGreaterThanOrEqualTo(16);

        Raid.Title.Should().Be("Raiders at a Holding");
        Customs.Title.Should().Be("Customs at a Holding");
        Raid.Choices.Select(choice => choice.Text).Should().Equal("Send the city's guards", "Leave it to the owner");
        Customs.Choices.Select(choice => choice.Text).Should().Equal("Pay the fine from the treasury", "Let the owner pay");
    }

    [Test]
    public void TheyAreRaisedByTheHoldingsEvent_NeverDrawnAtRandom_AndNeverFollowUps()
    {
        foreach (var detail in new[] { Raid, Customs })
        {
            detail.IsEventDriven.Should().BeTrue();
            detail.IsFollowUp.Should().BeFalse();
        }

        var context = new SituationContext { CityLevel = 5 };
        SituationRules.SelectEligible(SituationCatalog.All.Values, context, new HashSet<SituationType>())
            .Select(detail => detail.Type).Should().NotContain(new[] { SituationType.HoldingRaid, SituationType.HoldingCustoms });
    }

    [Test]
    public void SendingHelp_CostsFiveThousand_AndLeavingItCostsNothing()
    {
        var help = Choice(Raid, "Send the city's guards");
        help.Cost.Should().Be(HoldingSituationRules.RaidHelpCost).And.Be(5000);
        help.Effects.Should().Contain(effect => effect.Type == SituationEffectType.HoldingHelp);
        SituationRules.GetGain(help.Effects).Should().Be(0);

        var leave = Choice(Raid, "Leave it to the owner");
        leave.Cost.Should().Be(0);
        leave.Effects.Should().BeEmpty();
        Raid.IgnoredEffects.Should().BeEmpty();
    }

    [Test]
    public void PayingTheFine_CostsWhatTheFineIs_UpToTheLimitOnASituationsCost()
    {
        var pay = Choice(Customs, "Pay the fine from the treasury");
        pay.Effects.Should().ContainSingle().Which.Type.Should().Be(SituationEffectType.HoldingFine);

        pay.Cost.Should().Be(0, "the cost is read from the fine when the choice is picked");
        pay.GetCost(5250).Should().Be(5250);
        pay.GetCost(0).Should().Be(0);
        pay.GetCost(24633).Should().Be(SituationRules.MaxCost);
        SituationRules.GetRequestedChange(pay.Effects, 7875).Should().Be(-7875);
        SituationRules.GetRequestedChange(pay.Effects, 40000).Should().Be(-SituationRules.MaxCost);
        SituationRules.GetGain(pay.Effects).Should().Be(0);

        var leave = Choice(Customs, "Let the owner pay");
        leave.GetCost(5250).Should().Be(0);
        Customs.IgnoredEffects.Should().BeEmpty();
    }

    [Test]
    public void TheFineAHoldingCanOwe_FitsTheBandsOrIsPaidInPartAtTheLimit()
    {
        // A full warehouse of banned stock at the highest holding level is the biggest fine a holding can be handed.
        foreach (var type in new[] { HoldingType.SpiceLab, HoldingType.ExcavationDig })
        {
            var detail = HoldingRules.GetDetail(type);
            detail.CustomsInspections.Should().BeTrue();
            var holding = Holding(type);
            holding.Level = HoldingTuning.MaximumLevel;
            HoldingRules.AddStock(holding.Warehouse, detail.Output, HoldingRules.GetStorageVolume(holding, detail) / TradeLookupVolume(detail));

            var fine = (int)Math.Round(HoldingRules.GetBannedValue(holding.Warehouse) * HoldingTuning.CustomsFineRate, MidpointRounding.AwayFromZero);
            HoldingSituationRules.GetFineRelief(fine).Should().Be(Math.Min(fine, SituationRules.MaxCost));
        }
    }

    private static int TradeLookupVolume(HoldingDetail detail) =>
        Math.Max(1, SWLOR.Game.Server.Service.TradeService.TradeLookup.GetVolume(detail.Output));

    [Test]
    public void TheTextsNameNoPlayer_AndArePlainAscii()
    {
        foreach (var detail in new[] { Raid, Customs })
        {
            var texts = new List<string> { detail.Title, detail.Text, detail.IgnoredGazetteText };
            texts.AddRange(detail.Choices.SelectMany(choice => new[] { choice.Text, choice.GazetteText }));
            foreach (var text in texts)
            {
                text.Should().NotBeNullOrWhiteSpace();
                text.All(character => character >= ' ' && character <= '~').Should().BeTrue(text);
            }
        }

        var holding = Holding();
        holding.RaidPending = true;
        holding.RaidDeadline = Now.AddDays(2);
        var note = HoldingSituationRules.BuildNote(SituationType.HoldingRaid, HoldingRules.GetDetail(holding.Type), "Smuggler's Moon", holding);
        note.Should().Contain("Spice Lab").And.Contain("Smuggler's Moon").And.NotContain(holding.OwnerPlayerId);
    }

    // What raises them

    [Test]
    public void ARaidThatBeganRaisesTheRaidSituation_AndAFineThatWasHandedOutTheCustomsOne()
    {
        var holding = Holding();

        HoldingSituationRules.GetNewSituations(false, false, holding).Should().BeEmpty();

        holding.RaidPending = true;
        HoldingSituationRules.GetNewSituations(false, false, holding).Should().Equal(SituationType.HoldingRaid);
        HoldingSituationRules.GetNewSituations(true, false, holding).Should().BeEmpty("the raid was already there");

        holding.CustomsFine = 5000;
        HoldingSituationRules.GetNewSituations(false, false, holding).Should().Equal(SituationType.HoldingRaid, SituationType.HoldingCustoms);
        HoldingSituationRules.GetNewSituations(true, true, holding).Should().BeEmpty();
        HoldingSituationRules.GetNewSituations(false, true, holding).Should().Equal(SituationType.HoldingRaid);
    }

    [Test]
    public void OnlyARaidAndACustomsInspectionRaiseASituation()
    {
        HoldingSituationRules.GetSituationType(HoldingEventType.Raid).Should().Be(SituationType.HoldingRaid);
        HoldingSituationRules.GetSituationType(HoldingEventType.Customs).Should().Be(SituationType.HoldingCustoms);
        HoldingSituationRules.GetSituationType(HoldingEventType.Boom).Should().Be(SituationType.Invalid);
        HoldingSituationRules.GetSituationType(HoldingEventType.Setback).Should().Be(SituationType.Invalid);
        HoldingSituationRules.GetSituationType(HoldingEventType.None).Should().Be(SituationType.Invalid);
    }

    [Test]
    public void TheSituationRunsOutAtTheEventsOwnDeadline_IfThatComesFirst()
    {
        HoldingSituationRules.GetExpiryUtc(Now, Now.AddDays(2)).Should().Be(Now.AddDays(2));
        HoldingSituationRules.GetExpiryUtc(Now, Now.AddHours(30)).Should().Be(Now.AddHours(30));
        HoldingSituationRules.GetExpiryUtc(Now, Now.AddDays(10)).Should().Be(Now.AddHours(SituationRules.ExpiryHours));
        (HoldingTuning.CustomsAnswerDays * 24).Should().BeLessThanOrEqualTo(SituationRules.ExpiryHours);
        (HoldingTuning.RaidAnswerDays * 24).Should().BeLessThanOrEqualTo(SituationRules.ExpiryHours);
    }

    [Test]
    public void ASettledEventClosesItsSituation_ButADeadEventIsNeverStillPending()
    {
        var holding = Holding();
        holding.RaidPending = true;
        HoldingSituationRules.IsStillPending(SituationType.HoldingRaid, holding).Should().BeTrue();
        HoldingSituationRules.IsStillPending(SituationType.HoldingCustoms, holding).Should().BeFalse();

        holding.RaidPending = false;
        HoldingSituationRules.IsStillPending(SituationType.HoldingRaid, holding).Should().BeFalse();

        holding.CustomsFine = 100;
        HoldingSituationRules.IsStillPending(SituationType.HoldingCustoms, holding).Should().BeTrue();
        holding.CustomsFine = 0;
        HoldingSituationRules.IsStillPending(SituationType.HoldingCustoms, holding).Should().BeFalse();

        HoldingSituationRules.IsStillPending(SituationType.HoldingRaid, null).Should().BeFalse("a foreclosed holding has nothing left to help");
        HoldingSituationRules.IsStillPending(SituationType.PestsInTheGardens, holding).Should().BeFalse();
    }

    [Test]
    public void TheCityPaysTheFine_InFullOrInPart_AndTheHoldingKeepsTheRest()
    {
        var holding = Holding();
        holding.CustomsFine = 7875;

        HoldingSituationRules.ApplyFineRelief(holding, 5000).Should().Be(5000);
        holding.CustomsFine.Should().Be(2875);

        HoldingSituationRules.ApplyFineRelief(holding, 9999).Should().Be(2875, "never more than the fine");
        holding.CustomsFine.Should().Be(0);

        HoldingSituationRules.ApplyFineRelief(holding, 100).Should().Be(0);
        HoldingSituationRules.BuildFineLog("Dantari", 5000, 2875).Should().Contain("2875 credits are still owed");
        HoldingSituationRules.BuildFineLog("Dantari", 2875, 0).Should().Contain("paid the 2875 credit fine");
        HoldingSituationRules.BuildHelpLog("Dantari").Should().Contain("drove the raiders off");
    }

    // The effects and their text

    [Test]
    public void TheNewEffectsAreDescribedForTheOfficials()
    {
        SituationRules.DescribeEffects(Choice(Raid, "Send the city's guards").Effects).Should().Contain("guards drive the raiders off").And.Contain("Costs 5,000 cr");
        SituationRules.DescribeEffects(Choice(Customs, "Pay the fine from the treasury").Effects, false, 5250).Should().Contain("5,250 cr");
        SituationRules.DescribeEffects(Choice(Customs, "Pay the fine from the treasury").Effects, false, 40000).Should().Contain("15,000 cr");
        SituationRules.DescribeEffects(Choice(Customs, "Pay the fine from the treasury").Effects).Should().Contain("up to 15,000 cr");
    }

    // A station

    [Test]
    public void AnOrbitStationsCitizensGetTheSituationsForHoldingsOnTheirOrbitsPlanet_AWildSpaceStationNever()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => !x.IsWildSpace))
        {
            IndustrialCharterRules.IsOnCityPlanet(anchor.Exchange, anchor.Exchange, false).Should().BeTrue(anchor.Id);
        }

        foreach (var anchor in StationTuning.Anchors.Where(x => x.IsWildSpace))
        {
            IndustrialCharterRules.IsOnCityPlanet(anchor.Exchange, anchor.Exchange, true).Should().BeFalse(anchor.Id);
        }
    }

    // Wiring

    [Test]
    public void AHoldingAdvanceRaisesSituations_FromWhatItLeftPending()
    {
        var process = Source("Service", "PlanetaryHoldings.cs");
        var start = process.IndexOf("private static bool Process(PlanetaryHolding holding, uint owner)", StringComparison.Ordinal);
        var body = process[start..process.IndexOf("private static void Foreclose(", start, StringComparison.Ordinal)];

        body.Should().Contain("var hadRaid = holding.RaidPending;");
        body.Should().Contain("var hadFine = holding.CustomsFine > 0;");
        body.Should().Contain("HoldingCityAffairs.RaiseSituations(holding, detail, hadRaid, hadFine, now);");
        body.IndexOf("DB.Set(holding)", StringComparison.Ordinal).Should().BeLessThan(body.IndexOf("RaiseSituations(", StringComparison.Ordinal));
    }

    [Test]
    public void TheCityAffairsServiceAppliesTheNewEffects_AndRaisesTheSituationsUnderTheUsualLimits()
    {
        var service = Source("Service", "CityAffairs.cs");

        service.Should().Contain("effect.Type == SituationEffectType.HoldingHelp");
        service.Should().Contain("effect.Type == SituationEffectType.HoldingFine");
        service.Should().Contain("SituationRules.GetRequestedChange(effects, subjectCredits)");

        var raise = service[service.IndexOf("public static bool RaiseEvent(", StringComparison.Ordinal)..];
        raise.Should().Contain("detail.IsEventDriven");
        raise.Should().Contain("SituationRules.CanDrawNew(");
        raise.Should().Contain("SituationRules.GetOpenTypes(situations).Contains(type)");

        service.Should().Contain("HoldingCityAffairs.CloseIfSettled(situation, now)");
    }

    [Test]
    public void ARaisedSituationOnlyReachesACityOnTheHoldingsPlanet()
    {
        var raise = Source("Service", "HoldingCityAffairs.cs");

        raise.Should().Contain("IndustrialCharterRules.IsOnCityPlanet(detail.Exchange, EdictEffects.GetCityExchange(city), Edict.IsWildSpaceStation(city))");
        raise.Should().Contain("Edict.GetLiveCity(owner.CitizenPropertyId)");
    }
}
