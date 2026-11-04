using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.HoldingService;
using SWLOR.Game.Server.Service.IndustryService;
using SWLOR.Game.Server.Service.StationService;
using SWLOR.Game.Server.Service.TradeService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The Industrial Charter (Player City Revamp, "Planetary industry"): half the setback chance and half the repair goods for a
/// citizen's holding on the city's planet, settled across every change of the Edict or the citizenship. The roll bands and the
/// repair rounding follow the shape agreed with the space revamp.
/// </summary>
public class IndustrialCharterTests
{
    private static readonly DateTime Start = new(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedRandom : System.Random
    {
        private readonly double _value;
        public FixedRandom(double value) => _value = value;
        protected override double Sample() => _value;
        public override double NextDouble() => _value;
    }

    private static HoldingDetail Detail(HoldingType type) => HoldingRules.GetDetail(type);

    private static PlanetaryHolding New(HoldingType type) => HoldingRules.Create("player", Detail(type), "korriban_dock", Start);

    private static readonly HoldingModifiers Charter = IndustrialCharterRules.Modifiers;

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

    private static string Method(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"{signature} should exist");
        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var index = open; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        throw new InvalidOperationException($"{signature} has no closing brace.");
    }

    // The roll bands

    [Test]
    public void TheCharter_HalvesTheSetbackChance()
    {
        IndustrialCharterRules.SetbackChanceScale.Should().Be(0.5);
        IndustrialCharterRules.RepairUnitsScale.Should().Be(0.5);
        Charter.SetbackChanceScale.Should().Be(0.5);
        HoldingModifiers.None.IsNone.Should().BeTrue();
        Charter.IsNone.Should().BeFalse();
    }

    [Test]
    public void OnlyTheSetbackBandShrinks_TheOtherBandsKeepTheirChancesAndTheFreedShareIsNoEvent()
    {
        var dig = Detail(HoldingType.ExcavationDig);
        dig.CustomsInspections.Should().BeTrue();

        Dictionary<HoldingEventType, int> Count(HoldingModifiers modifiers)
        {
            var counts = new Dictionary<HoldingEventType, int>();
            for (var step = 0; step < 100000; step++)
            {
                var result = HoldingRules.RollEvent(dig, step / 100000.0, modifiers);
                counts[result] = counts.GetValueOrDefault(result) + 1;
            }

            return counts;
        }

        var plain = Count(null);
        var chartered = Count(Charter);

        plain[HoldingEventType.Setback].Should().BeCloseTo(5000, 2);
        chartered[HoldingEventType.Setback].Should().BeCloseTo(2500, 2);
        chartered[HoldingEventType.Boom].Should().Be(plain[HoldingEventType.Boom]);
        chartered[HoldingEventType.Raid].Should().Be(plain[HoldingEventType.Raid]);
        chartered[HoldingEventType.Customs].Should().Be(plain[HoldingEventType.Customs]);
        chartered[HoldingEventType.None].Should().Be(plain[HoldingEventType.None] + plain[HoldingEventType.Setback] - chartered[HoldingEventType.Setback]);
    }

    [Test]
    public void TheOtherBandsStayWhereTheyWere_SoAPinnedRollGivesTheSameEvent()
    {
        var dig = Detail(HoldingType.ExcavationDig);

        HoldingRules.RollEvent(dig, 0.05, Charter).Should().Be(HoldingEventType.Boom);
        HoldingRules.RollEvent(dig, 0.13, Charter).Should().Be(HoldingEventType.Setback);
        HoldingRules.RollEvent(dig, 0.16, null).Should().Be(HoldingEventType.Setback);
        HoldingRules.RollEvent(dig, 0.16, Charter).Should().Be(HoldingEventType.None);
        HoldingRules.RollEvent(dig, 0.18, Charter).Should().Be(HoldingEventType.Raid);
        HoldingRules.RollEvent(dig, 0.25, Charter).Should().Be(HoldingEventType.Customs);
        HoldingRules.RollEvent(Detail(HoldingType.GrainFarm), 0.25, Charter).Should().Be(HoldingEventType.None);
    }

    [Test]
    public void DefaultModifiers_RollExactlyAsBefore()
    {
        foreach (var type in Enum.GetValues<HoldingType>().Where(x => HoldingRules.GetDetail(x) != null))
        {
            for (var step = 0; step < 1000; step++)
            {
                var roll = step / 1000.0;
                HoldingRules.RollEvent(Detail(type), roll, HoldingModifiers.None).Should().Be(HoldingRules.RollEvent(Detail(type), roll));
            }
        }
    }

    // The repair goods

    [Test]
    public void RepairGoods_AreHalvedAndRoundedUp()
    {
        HoldingRules.GetRepairUnits(Detail(HoldingType.ExcavationDig), Charter).Should().Be(19);
        HoldingRules.GetRepairUnits(Detail(HoldingType.SpiceLab), Charter).Should().Be(6);
        HoldingRules.GetRepairUnits(Detail(HoldingType.ExcavationDig)).Should().Be(37);
        HoldingRules.GetRepairUnits(Detail(HoldingType.ExcavationDig), HoldingModifiers.None).Should().Be(37);

        foreach (var detail in Enum.GetValues<HoldingType>().Select(HoldingRules.GetDetail).Where(x => x != null))
        {
            HoldingRules.GetRepairUnits(detail, Charter).Should().Be((detail.RepairUnits + 1) / 2, detail.Name);
        }
    }

    [Test]
    public void ASetbackRolledUnderTheCharter_NeedsTheReducedGoods_InTheRecordAndTheLog()
    {
        var holding = New(HoldingType.ExcavationDig);
        var detail = Detail(HoldingType.ExcavationDig);

        var result = HoldingRules.Advance(holding, detail, Start.AddDays(3), new FixedRandom(0.13), Charter);

        holding.Setback.Should().Be(detail.Setback);
        holding.RepairUnitsNeeded.Should().Be(19);
        result.Events.Should().Contain(text => text.Contains("19 "));
        result.Events.Should().NotContain(text => text.Contains("37 "));
    }

    [Test]
    public void ASetbackRolledUnderTheCharter_KeepsItsReducedNeedAfterTheEdictEnds()
    {
        var holding = New(HoldingType.ExcavationDig);
        var detail = Detail(HoldingType.ExcavationDig);
        HoldingRules.Advance(holding, detail, Start.AddDays(3), new FixedRandom(0.13), Charter);

        // The Edict is repealed: the days that follow roll without it, and the stored need does not change.
        HoldingRules.Advance(holding, detail, Start.AddDays(10), new FixedRandom(0.99));
        holding.RepairUnitsNeeded.Should().Be(19);
        HoldingRules.GetNeeds(holding, detail).Single(x => x.Commodity == detail.RepairGood).Wanted.Should().BeGreaterThanOrEqualTo(19);

        // Delivering the reduced goods repairs it.
        HoldingRules.Deliver(holding, detail, detail.RepairGood, 19, Start.AddDays(10), new HoldingAdvanceResult());
        holding.Setback.Should().Be(HoldingSetbackType.Invalid);
        holding.RepairUnitsNeeded.Should().Be(0);
    }

    [Test]
    public void ASetbackRolledWithoutTheCharter_KeepsItsFullNeedWhileTheCharterRuns()
    {
        var holding = New(HoldingType.ExcavationDig);
        var detail = Detail(HoldingType.ExcavationDig);
        HoldingRules.Advance(holding, detail, Start.AddDays(3), new FixedRandom(0.13));

        HoldingRules.Advance(holding, detail, Start.AddDays(6), new FixedRandom(0.99), Charter);

        holding.RepairUnitsNeeded.Should().Be(37);
    }

    // Settle, then switch

    [Test]
    public void SettleThenSwitch_AChangeInTheMiddleSplitsTheDaysAtTheChange()
    {
        var detail = Detail(HoldingType.ExcavationDig);

        // A roll of 0.15 is a setback without the Charter and no event with it. Events roll on days 3, 6, 9 and 12.
        // The Charter stands until the middle of day 4, then ends.
        var split = New(HoldingType.ExcavationDig);
        HoldingSettlementRules.Advance(split, detail, Start.AddDays(12), new FixedRandom(0.15),
            at => at < Start.AddDays(4.5) ? Charter : HoldingModifiers.None,
            new[] { Start.AddDays(4.5) });
        split.Setback.Should().Be(detail.Setback);
        split.Log.Should().Contain(line => line.StartsWith($"{Start.AddDays(6):yyyy-MM-dd}") && line.Contains("Output stops"));
        split.Log.Should().NotContain(line => line.StartsWith($"{Start.AddDays(3):yyyy-MM-dd}") && line.Contains("Output stops"));

        // Without the change time the current state would reach back over every day.
        var current = New(HoldingType.ExcavationDig);
        HoldingSettlementRules.Advance(current, detail, Start.AddDays(12), new FixedRandom(0.15), _ => HoldingModifiers.None, null);
        current.Log.Should().Contain(line => line.StartsWith($"{Start.AddDays(3):yyyy-MM-dd}") && line.Contains("Output stops"));
    }

    [Test]
    public void SettleThenSwitch_ACharterThatStartsMidwayOnlyCountsAfterItStarts()
    {
        var detail = Detail(HoldingType.ExcavationDig);

        // A setback roll of 0.13 needs 37 goods without the Charter and 19 with it. The first event rolls on day 3.
        var late = New(HoldingType.ExcavationDig);
        HoldingSettlementRules.Advance(late, detail, Start.AddDays(4), new FixedRandom(0.13),
            at => at >= Start.AddDays(3.5) ? Charter : HoldingModifiers.None,
            new[] { Start.AddDays(3.5) });
        late.RepairUnitsNeeded.Should().Be(37);

        var early = New(HoldingType.ExcavationDig);
        HoldingSettlementRules.Advance(early, detail, Start.AddDays(4), new FixedRandom(0.13),
            at => at >= Start.AddDays(2.5) ? Charter : HoldingModifiers.None,
            new[] { Start.AddDays(2.5) });
        early.RepairUnitsNeeded.Should().Be(19);
    }

    [Test]
    public void SettleThenSwitch_ChangeTimesOutsideTheStretchAreIgnored_AndTheDaysAreCountedOnce()
    {
        var detail = Detail(HoldingType.ExcavationDig);
        var holding = New(HoldingType.ExcavationDig);

        var result = HoldingSettlementRules.Advance(holding, detail, Start.AddDays(9), new FixedRandom(0.99),
            _ => HoldingModifiers.None,
            new[] { Start.AddDays(-5), Start.AddDays(2.5), Start.AddDays(2.5), Start.AddDays(30) });

        result.Days.Should().Be(9);
        holding.LastProcessedAt.Should().Be(Start.AddDays(9));

        var whole = New(HoldingType.ExcavationDig);
        HoldingRules.Advance(whole, detail, Start.AddDays(9), new FixedRandom(0.99));
        holding.UpkeepOwed.Should().Be(whole.UpkeepOwed);
        holding.Warehouse.Should().BeEquivalentTo(whole.Warehouse);
    }

    // Who gets it

    [Test]
    public void TheCharter_NeedsTheEdict_ACitizenInGoodStanding_AndAHoldingOnTheCitysPlanet()
    {
        IndustrialCharterRules.GetModifiers(true, true, true).Should().BeSameAs(IndustrialCharterRules.Modifiers);

        IndustrialCharterRules.GetModifiers(false, true, true).IsNone.Should().BeTrue();
        IndustrialCharterRules.GetModifiers(true, false, true).IsNone.Should().BeTrue();
        IndustrialCharterRules.GetModifiers(true, true, false).IsNone.Should().BeTrue();
    }

    [Test]
    public void APlanetCity_AppliesItToHoldingsOnItsOwnPlanetOnly()
    {
        IndustrialCharterRules.IsOnCityPlanet(TradeExchangeType.Korriban, TradeExchangeType.Korriban, false).Should().BeTrue();
        IndustrialCharterRules.IsOnCityPlanet(TradeExchangeType.Viscara, TradeExchangeType.Korriban, false).Should().BeFalse();
        IndustrialCharterRules.IsOnCityPlanet(TradeExchangeType.Korriban, TradeExchangeType.Invalid, false).Should().BeFalse();
    }

    [Test]
    public void AnOrbitStation_AppliesItToItsOrbitsPlanet_AndAWildSpaceStationNeverHasIt()
    {
        foreach (var anchor in StationTuning.Anchors.Where(x => !x.IsWildSpace))
        {
            IndustrialCharterRules.IsOnCityPlanet(anchor.Exchange, anchor.Exchange, false).Should().BeTrue(anchor.Id);
            var otherPlanet = HoldingCatalogExchanges().First(x => x != anchor.Exchange);
            IndustrialCharterRules.IsOnCityPlanet(otherPlanet, anchor.Exchange, false).Should().BeFalse(anchor.Id);
        }

        foreach (var anchor in StationTuning.Anchors.Where(x => x.IsWildSpace))
        {
            // A Wild Space station trades through the nearest open port, but it has no planet below it.
            IndustrialCharterRules.IsOnCityPlanet(anchor.Exchange, anchor.Exchange, true).Should().BeFalse(anchor.Id);
        }
    }

    private static IEnumerable<TradeExchangeType> HoldingCatalogExchanges() =>
        Enum.GetValues<HoldingType>().Select(HoldingRules.GetDetail).Where(x => x != null).Select(x => x.Exchange).Distinct();

    // Where the change points are

    [Test]
    public void HoldingsAdvanceThroughTheCityHook_AndTheOwnerIsToldAboutCityEvents()
    {
        var process = Method(Source("Service", "PlanetaryHoldings.cs"), "private static bool Process(PlanetaryHolding holding, uint owner)");

        process.Should().Contain("EdictEffects.AdvanceHolding(holding, detail,");
        process.Should().NotContain("HoldingRules.Advance(");
    }

    [Test]
    public void TheEdictSettlesHoldingsBeforeItChanges()
    {
        var edict = Source("Service", "Edict.cs");

        var enact = Method(edict, "public static EdictRefusal Enact(");
        enact.Should().Contain("IndustrialCharter.SettleCity(city, aboutToEnact: true)");
        enact.IndexOf("IndustrialCharter.SettleCity", StringComparison.Ordinal).Should().BeLessThan(enact.IndexOf("EdictChanges.Enact(", StringComparison.Ordinal));

        var repeal = Method(edict, "public static EdictRefusal Repeal(");
        repeal.Should().Contain("IndustrialCharter.SettleCity(city)");
        repeal.IndexOf("IndustrialCharter.SettleCity", StringComparison.Ordinal).Should().BeLessThan(repeal.IndexOf("EdictChanges.Repeal(", StringComparison.Ordinal));

        var platform = Method(edict, "public static EdictPlatformEnactment EnactPlatform(");
        platform.IndexOf("IndustrialCharter.SettleCity", StringComparison.Ordinal).Should().BeLessThan(platform.IndexOf("EdictChanges.EnactPlatform(", StringComparison.Ordinal));

        var pauses = Method(edict, "public static void ApplyCityLevelPauses(");
        pauses.IndexOf("IndustrialCharter.SettleCity", StringComparison.Ordinal).Should().BeLessThan(pauses.IndexOf("EdictChanges.ApplyCityLevelPauses(", StringComparison.Ordinal));

        var deleted = Method(edict, "public static void OnCityDeleted(");
        deleted.IndexOf("IndustrialCharter.SettleCity", StringComparison.Ordinal).Should().BeLessThan(deleted.IndexOf("EdictChanges.RemoveAllianceNaming(", StringComparison.Ordinal));
    }

    [Test]
    public void ThePauseAndResumePathsSettleBeforeTheyChange()
    {
        var treasury = Method(Source("Service", "CityTreasury.cs"), "public static bool PayUpkeep(WorldProperty city, DateTime nowUtc)");
        treasury.IndexOf("IndustrialCharter.SettleCity(city)", StringComparison.Ordinal).Should().BeGreaterThan(0)
            .And.BeLessThan(treasury.IndexOf("CityTreasuryRules.PayUpkeep(city)", StringComparison.Ordinal));

        var property = Source("Service", "Property.cs");
        var processing = Method(property, "private static void ProcessCities()");
        processing.IndexOf("IndustrialCharter.SettleCity(city)", StringComparison.Ordinal).Should().BeGreaterThan(0)
            .And.BeLessThan(processing.IndexOf("ProcessCityCitizenRequirement(now, city)", StringComparison.Ordinal));

        var deletion = Method(property, "public static void DeleteProperty(WorldProperty property)");
        deletion.IndexOf("IndustrialCharter.SettleCity(property)", StringComparison.Ordinal).Should().BeGreaterThan(0)
            .And.BeLessThan(deletion.IndexOf("citizen.CitizenPropertyId = string.Empty", StringComparison.Ordinal));
    }

    [Test]
    public void ACitizenshipOrTaxChange_SettlesTheOwnersHoldingsFirst()
    {
        var citizenship = Source("Feature", "GuiDefinition", "ViewModel", "ManageCitizenshipViewModel.cs");

        citizenship.Split("IndustrialCharter.SettleOwner(playerId)").Length.Should().Be(4, "leaving, joining and paying taxes each settle once");

        var leave = citizenship.IndexOf("IndustrialCharter.SettleOwner(playerId)", StringComparison.Ordinal);
        leave.Should().BeLessThan(citizenship.IndexOf("dbPlayer.CitizenPropertyId = string.Empty", StringComparison.Ordinal));

        var join = citizenship.IndexOf("IndustrialCharter.SettleOwner(playerId)", leave + 1, StringComparison.Ordinal);
        join.Should().BeLessThan(citizenship.IndexOf("dbPlayer.CitizenPropertyId = _cityPropertyId", StringComparison.Ordinal));

        var tax = citizenship.IndexOf("IndustrialCharter.SettleOwner(playerId)", join + 1, StringComparison.Ordinal);
        tax.Should().BeLessThan(citizenship.IndexOf("dbPlayer.PropertyOwedTaxes = 0;", tax, StringComparison.Ordinal));
    }

    [Test]
    public void TheCityHook_UsesTheCitizensCityOnlyWhenItHoldsTheCharter_AndSplitsAtTheWaitingPeriod()
    {
        var hook = Method(Source("Service", "EdictEffects.cs"), "public static HoldingAdvanceResult AdvanceHolding(");

        hook.Should().Contain("EdictType.IndustrialCharter");
        hook.Should().Contain("IndustrialCharterRules.IsOnCityPlanet(");
        hook.Should().Contain("EdictRules.CitizenDelayDays");
        hook.Should().Contain("SituationPauseEndsUtc");
        hook.Should().Contain("HoldingSettlementRules.Advance(");
        hook.Should().Contain("return HoldingRules.Advance(holding, detail, nowUtc, random);");
    }
}
