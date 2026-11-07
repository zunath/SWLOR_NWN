using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The DM Tools Space tab's derelict, signature, Rift Pocket and cloak tools: the rules and texts, and that the buttons
/// are wired and the tab keeps the Placeables tab's panel size (the #3000 regression).
/// </summary>
public class DmSpaceToolTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static string Root()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (Directory.Exists(Path.Combine(directory.FullName, "Module", "git")) && Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static string ReadSource(params string[] relativePath) =>
        File.ReadAllText(relativePath.Aggregate(Path.Combine(Root(), "SWLOR.Game.Server"), Path.Combine));

    // Pickers

    [Test]
    public void TheSignaturePicker_OffersEverySiteKindButRifts()
    {
        DmSpaceToolRules.SignatureChoices.Select(x => x.Type).Should().BeEquivalentTo(new[]
        {
            SignatureType.CombatSite, SignatureType.DataSite, SignatureType.CacheSite, SignatureType.GasSite, SignatureType.Derelict,
        });
        DmSpaceToolRules.SignatureChoices.Select(x => x.Label).Should().OnlyHaveUniqueItems();
        DmSpaceToolRules.GetSignatureChoice(-3).Type.Should().Be(SignatureType.CombatSite);
        DmSpaceToolRules.GetSignatureChoice(4).Type.Should().Be(SignatureType.Derelict);
        DmSpaceToolRules.GetSignatureChoice(99).Type.Should().Be(SignatureType.Derelict);
    }

    [Test]
    public void ThePocketPicker_HasOneEntryPerRiftKey()
    {
        DmSpaceToolRules.RiftTierLabels.Should().Equal("Tier 1", "Tier 2", "Tier 3", "Tier 4", "Tier 5", "Tier 5.5");
        for (var index = 0; index < RiftRunTuning.Keys.Count; index++)
            DmSpaceToolRules.GetRiftKey(index).Should().BeSameAs(RiftRunTuning.Keys[index]);

        DmSpaceToolRules.GetRiftKey(-1).Should().BeSameAs(RiftRunTuning.Keys[0]);
        DmSpaceToolRules.GetRiftKey(50).Should().BeSameAs(RiftRunTuning.Keys[^1]);
    }

    // Placing

    [Test]
    public void APlacement_NeedsASpaceAreaThatHoldsSignatures()
    {
        DmSpaceToolRules.GetPlaceRefusal(false, "viscaraorbit").Should().Be("You must be in a space area.");
        DmSpaceToolRules.GetPlaceRefusal(true, RiftRunTuning.TemplateResref).Should().Contain("can only be placed in");
        DmSpaceToolRules.GetPlaceRefusal(true, "not_an_area").Should().Contain("can only be placed in");

        foreach (var area in ScanningTuning.Areas)
            DmSpaceToolRules.GetPlaceRefusal(true, area.AreaResref).Should().BeEmpty(area.AreaResref);
    }

    [Test]
    public void ThePlacementReport_SaysWhetherTheSiteIsOpen()
    {
        var derelict = SignatureSiteRules.Create("a", "viscaraorbit", 1, SignatureType.Derelict, 10, 20, 0, Now, boardable: true);
        DmSpaceToolRules.DescribePlaced(derelict, true).Should().Be("Placed a tier 1 boardable derelict where you pointed. Its site is open.");

        var data = SignatureSiteRules.Create("b", "space_dathomir", 5, SignatureType.DataSite, 10, 20, 0, Now);
        DmSpaceToolRules.DescribePlaced(data, false).Should().Be("Placed a hidden tier 5 data site where you pointed. A Probe Launcher can find it.");

        var wild = SignatureSiteRules.Create("c", "space_wspulsar", 5, SignatureType.CombatSite, 10, 20, 0, Now, wildSpace: true);
        DmSpaceToolRules.DescribePlaced(wild, false).Should().Contain("tier 5.5");
    }

    [Test]
    public void ADerelictMadeBoardable_IsBoardableAndNeverContested()
    {
        var derelict = SignatureSiteRules.Create("a", "moncalaorbit", 2, SignatureType.Derelict, 1, 2, 0, Now, boardable: true, contested: false);
        derelict.Boardable.Should().BeTrue();
        derelict.Contested.Should().BeFalse();

        SignatureSiteRules.Create("b", "moncalaorbit", 2, SignatureType.DataSite, 1, 2, 0, Now, boardable: true).Boardable.Should().BeFalse();
    }

    // Rift Pockets

    [Test]
    public void TheRunLine_ShowsTierShipsChamberPhaseAndTime()
    {
        var run = new RiftRun
        {
            Key = RiftRunTuning.Keys[2],
            Ships = 2,
            Room = 1,
            Phase = RiftRunPhase.Fighting,
            StartedAt = Now.AddSeconds(-100),
        };

        DmSpaceToolRules.DescribeRiftRun(run, Now).Should().Be("Tier 3, 2 ship(s), chamber 2 of 3 (Shear Zone), fighting, 13:20 left");
    }

    [Test]
    public void TheRunSummary_CountsOpenPockets()
    {
        DmSpaceToolRules.DescribeRiftRuns(new List<RiftRun>()).Should().Be("No Rift Pockets are open.");
        DmSpaceToolRules.DescribeRiftRuns(new List<RiftRun> { new(), new() }).Should().Be($"2 of {RiftRunTuning.MaxRuns} Rift Pockets open.");
    }

    // Wiring

    [Test]
    public void TheButtons_AreWiredAndNoneIsDisabled()
    {
        var definition = ReadSource("Feature", "GuiDefinition", "DMToolsDefinition.cs");
        foreach (var call in new[]
                 {
                     "model.OnPlaceDerelict()", "model.OnPlaceSignature()", "model.OnOpenRiftPocket()",
                     "model.OnCloseRiftPocket()", "model.OnListRiftPockets()", "model.OnUncloakShip()",
                     "model.OnStartIncursion()", "model.OnStartConvoy()", "model.OnSpawnSpaceSquad()",
                 })
        {
            definition.Should().Contain(call);
        }

        definition.Should().NotContain("SetIsEnabled(false)").And.NotContain("Comes with derelicts");

        var model = ReadSource("Feature", "GuiDefinition", "ViewModel", "DMToolsViewModel.cs");
        model.Should().Contain("Space.DmPlaceSignature(").And.Contain("Space.DmOpenRiftPocket(").And.Contain("Space.DmCloseRiftPocket(")
            .And.Contain("Space.DmListRiftPockets()").And.Contain("Space.DmDecloakShip(");
    }

    [Test]
    public void TheSpaceTab_KeepsThePlaceablesPanelSizeAndRestoresItsPicks()
    {
        var definition = ReadSource("Feature", "GuiDefinition", "DMToolsDefinition.cs");
        definition.Should().Contain("SpacePanelWidth, SpacePanelHeight");
        definition.Should().Contain("private const float SpacePanelWidth = 1000f;").And.Contain("private const float PlaceablesPanelWidth = 1000f;");

        var model = ReadSource("Feature", "GuiDefinition", "ViewModel", "DMToolsViewModel.cs");
        var restore = model[model.IndexOf("private void RestoreTabPicks()", StringComparison.Ordinal)..];
        restore = restore[..restore.IndexOf("}", StringComparison.Ordinal)];
        restore.Should().Contain("SpaceSignatureIndex = SpaceSignatureIndex;").And.Contain("SpacePocketIndex = SpacePocketIndex;");
        model.Should().Contain("WatchOnClient(model => model.SpaceSignatureIndex);").And.Contain("WatchOnClient(model => model.SpacePocketIndex);");
    }

    [Test]
    public void ADmPocketOpening_SpendsNoKeyAndTheCloakToolOnlyEndsTheCloak()
    {
        var runs = ReadSource("Service", "SpaceRiftRuns.cs");
        runs.Should().Contain("if (GetIsObjectValid(item))").And.Contain("Item.ReduceItemStack(item, 1);");
        runs.Should().Contain("OpenRiftRun(user, RiftRunRules.FindKey(GetResRef(item)), item);");

        var tools = ReadSource("Service", "SpaceDmTools.cs");
        tools.Should().Contain("OpenRiftRun(target, key, OBJECT_INVALID)").And.Contain("EndCloak(ship,");
        tools.Should().Contain("RevealSite(signature, controlled, openToAll: true)");
    }
}
