using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Defects found by the W11r live-client pass: the conversation speaker name clipped to one line ("Docking Point: CZ-2",
/// "Requisitions Officer"), a DM-placed derelict that could not be boarded until a party scanned it, the DM Tools status
/// line cut off and showing "CombatSite", "1 pilots share the spoils", a station Shipyard that threw because its state shared
/// its building's id, and an Advanced deed's rare-trait line cut off in the recipe window.
/// </summary>
public class SpaceW11rLiveFixTests
{
    private static readonly DateTime Now = new(2026, 10, 29, 4, 0, 0, DateTimeKind.Utc);

    private static string Root()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "Module", "git")) && Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    // Conversation speaker name

    [TestCase("Docking Point: CZ-220", "Docking Point:", "CZ-220", "")]
    [TestCase("Requisitions Officer Mira Kell", "Requisitions", "Officer Mira Kell", "")]
    [TestCase("Docking Point: Smuggler's Moon Station", "Docking Point:", "Smuggler's Moon", "Station")]
    public void ALongSpeakerName_WrapsOntoTheRowsWithoutLosingWords(string name, string first, string second, string third)
    {
        var lines = ConversationViewModel.WrapSpeakerName(name);

        lines.Should().Equal(first, second, third);
        lines.Should().OnlyContain(x => x.Length <= ConversationViewModel.SpeakerNameLineCharacters);
        string.Join(" ", lines.Where(x => x.Length > 0)).Should().Be(name);
    }

    [Test]
    public void AShortSpeakerName_SitsOnTheMiddleRow()
    {
        ConversationViewModel.WrapSpeakerName("Dockhand").Should().Equal("", "Dockhand", "");
        ConversationViewModel.WrapSpeakerName("").Should().Equal("", "", "");
        ConversationViewModel.WrapSpeakerName(null).Should().Equal("", "", "");
    }

    [Test]
    public void AnOverlongSpeakerName_IsCutOnTheThirdRow_AndALongWordIsSplit()
    {
        var lines = ConversationViewModel.WrapSpeakerName("The Exceedingly Long Named Captain Of The Very Distant Outer Rim Frontier");
        lines.Should().HaveCount(3);
        lines[2].Should().EndWith("...");
        lines.Should().OnlyContain(x => x.Length <= ConversationViewModel.SpeakerNameLineCharacters);

        ConversationViewModel.WrapSpeakerName("Supercalifragilisticexpialidocious").Should()
            .Equal("Supercalifragilist", "icexpialidocious", "");
    }

    [Test]
    public void TheConversationWindow_DrawsTheSpeakerNameOnThreeLabelRows()
    {
        var source = ReadSource("Feature", "GuiDefinition", "ConversationWindowDefinition.cs");
        source.Should().Contain("model.SpeakerNameLine1").And.Contain("model.SpeakerNameLine2").And.Contain("model.SpeakerNameLine3");
        source.Should().NotContain("BindText(model => model.SpeakerName)", "a single NUI label clips the name to one line");

        ReadSource("Feature", "GuiDefinition", "ViewModel", "ConversationViewModel.cs").Should()
            .Contain("var speakerLines = WrapSpeakerName(SpeakerName);");
    }

    // DM-placed derelict

    [Test]
    public void ASiteOpenedForEveryone_CountsAsLocatedForEveryParty()
    {
        var derelict = SignatureSiteRules.Create("a", "viscaraorbit", 1, SignatureType.Derelict, 10, 20, 0, Now, boardable: true);
        var party = new[] { "player-a" };

        ScanningRules.IsPartyLocated(derelict, party).Should().BeFalse("an unscanned signature is not located");

        derelict.OpenToAll = true;
        ScanningRules.IsPartyLocated(derelict, party).Should().BeTrue("a DM's Place Derelict opens the site to everyone");
        ScanningRules.IsPartyLocated(null, party).Should().BeFalse();
    }

    [Test]
    public void AScannedSignature_IsLocatedAtTheInfoThreshold()
    {
        var derelict = SignatureSiteRules.Create("b", "viscaraorbit", 1, SignatureType.Derelict, 10, 20, 0, Now, boardable: true);
        ScanningRules.AddSignal(derelict, "player-a", derelict.SignalRequired);
        ScanningRules.IsPartyLocated(derelict, new[] { "player-b", "player-a" }).Should().BeTrue();
        ScanningRules.IsPartyLocated(derelict, new[] { "player-b" }).Should().BeFalse();
    }

    [Test]
    public void PlaceDerelict_OpensItsSiteToEveryone_AndBoardingUsesTheSharedRule()
    {
        ReadSource("Service", "SpaceDmTools.cs").Should().Contain("signature.OpenToAll = reveal;");
        ReadSource("Service", "SpaceDerelicts.cs").Should().Contain("PartyLocated = ScanningRules.IsPartyLocated(signature, party),");
    }

    // DM Tools texts and layout

    [TestCase(SignatureType.CombatSite, "combat site")]
    [TestCase(SignatureType.DataSite, "data site")]
    [TestCase(SignatureType.GasSite, "gas site")]
    public void ThePlacementReport_NamesTheSiteKindInWords(SignatureType type, string kind)
    {
        var signature = SignatureSiteRules.Create("c", "space_dathomir", 5, type, 10, 20, 0, Now);
        DmSpaceToolRules.DescribePlaced(signature, false).Should()
            .Be($"Placed a hidden tier 5 {kind} where you pointed. A Probe Launcher can find it.");
    }

    [Test]
    public void TheSpaceStatus_IsAWrappingTextElement()
    {
        var source = ReadSource("Feature", "GuiDefinition", "DMToolsDefinition.cs");
        var status = source[source.IndexOf("private static void AddSpaceStatus", StringComparison.Ordinal)..];
        status = status[..status.IndexOf("private static void AddSpaceShipsColumn", StringComparison.Ordinal)];
        status.Should().Contain("row.AddText()").And.Contain("BindText(model => model.SpaceStatus)");
        status.Should().NotContain("row.AddLabel()");
    }

    // Battle and incursion broadcasts

    [TestCase(1, "1 pilot shares the spoils.")]
    [TestCase(0, "0 pilots share the spoils.")]
    [TestCase(3, "3 pilots share the spoils.")]
    public void TheSpoilsLine_AgreesWithTheCount(int paid, string expected)
    {
        CapitalBattleRules.DescribeSpoilsShare(paid).Should().Be(expected);
    }

    [Test]
    public void BothBroadcasts_UseTheSpoilsLine()
    {
        ReadSource("Service", "SpaceCapitalBattles.cs").Should().Contain("CapitalBattleRules.DescribeSpoilsShare(paid)").And.NotContain("pilots share the spoils");
        ReadSource("Service", "SpaceIncursions.cs").Should().Contain("CapitalBattleRules.DescribeSpoilsShare(paid)").And.NotContain("pilots share the spoils");
    }

    // Station building state

    [Test]
    public void AStationBuildingsState_HasAKeyOfItsOwn()
    {
        StationBuildingState.KeyFor("building-1").Should().NotBe("building-1",
            "the DB's per-frame cache is keyed by id alone, so the state cannot share its building's id");
        StationBuildingState.KeyFor("building-1").Should().Be("station-building:building-1");
    }

    [Test]
    public void StationBuildings_ReadAndWriteTheStateByItsOwnKey()
    {
        var source = ReadSource("Service", "StationBuildings.cs");
        source.Should().Contain("DB.Get<StationBuildingState>(StationBuildingState.KeyFor(building.Id))");
        source.Should().Contain("Id = StationBuildingState.KeyFor(building.Id),");
        source.Should().Contain("BuildingId = building.Id,");
        source.Should().Contain("DB.Exists<StationBuildingState>(StationBuildingState.KeyFor(building.Id))");
        source.Should().Contain("!DB.Exists<WorldProperty>(beacon.BuildingId)");
        source.Should().NotContain("DB.Get<StationBuildingState>(building.Id)");
        source.Should().NotContain("DB.Exists<StationBuildingState>(building.Id)");
    }

    // Recipe window stand-in line

    [Test]
    public void TheStandInLine_WrapsOntoDetailRows()
    {
        var lines = ShipRareComponentRules.StandInLines("sd_aurekstrikefi", null);
        lines.Should().HaveCountGreaterThan(1);
        lines.Should().OnlyContain(x => x.Length <= ShipRareComponentRules.DetailLineCharacters);
        string.Join(" ", lines.Select(x => x.Trim())).Should().Be(ShipRareComponentRules.StandInText("sd_aurekstrikefi", null));
        lines[0].Should().StartWith("1x Tier 2 component with a rare trait");

        ShipRareComponentRules.StandInLines("sd_striker", null).Should().BeEmpty();
    }

    // Holding raiders

    [Test]
    public void Raiders_ArePlacedFromTheSiteTemplatesWalkmesh()
    {
        // A site is an instance whose resref ("nw_3") was never baked, so every pick fell back to the entrance and the
        // four raiders spawned on top of the arriving player (W11r).
        var holdings = ReadSource("Service", "PlanetaryHoldings.cs");
        holdings.Should().Contain("Walkmesh.GetRandomLocation(site, detail.SiteAreaResref)");
        holdings.Should().NotContain("Walkmesh.GetRandomLocation(site);");

        var walkmesh = ReadSource("Service", "Walkmesh.cs");
        walkmesh.Should().Contain("public static Location GetRandomLocation(uint area, string templateResref)");
        walkmesh.Should().Contain("return GetRandomLocation(area, GetResRef(area));");
    }

    [Test]
    public void EveryOutdoorSiteTemplate_IsAModuleArea()
    {
        var sites = SWLOR.Game.Server.Feature.HoldingDefinition.HoldingCatalog.Holdings
            .Select(x => x.SiteAreaResref).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        sites.Should().NotBeEmpty();
        foreach (var site in sites)
            File.Exists(Path.Combine(Root(), "Module", "are", site + ".are.json")).Should().BeTrue(site);
    }
}
