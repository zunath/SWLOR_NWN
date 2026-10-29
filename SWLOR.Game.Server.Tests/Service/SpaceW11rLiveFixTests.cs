using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Defects found by the W11r live-client pass: the conversation speaker name clipped to one line ("Docking Point: CZ-2",
/// "Requisitions Officer"), a DM-placed derelict that could not be boarded until a party scanned it, the DM Tools status
/// line cut off and showing "CombatSite", and "1 pilots share the spoils".
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
}
