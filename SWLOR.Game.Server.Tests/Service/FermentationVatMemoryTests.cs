using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.EnzymeCultureService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// A Fermentation Vat remembers the colors it has copied, and picking it up must not make it forget. The colors are written to
/// the structure item as a local when the vat is picked up and read back when the item is placed again.
/// </summary>
public class FermentationVatMemoryTests
{
    private static readonly DateTime Started = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;

            directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
            return directory!.FullName;
        }
    }

    private static FermentationJob RunningJob(EnzymeType type, EnzymeColorType color, bool withStarter, DateTime? started = null) => new()
    {
        Id = FermentationJob.KeyFor("vat"),
        VatId = "vat",
        OwnerPlayerId = "player",
        Type = type,
        Color = color,
        StartedUtc = started ?? Started,
        CompleteUtc = EnzymeCultureRules.GetCompleteUtc(started ?? Started),
        StarterData = withStarter ? "serialized-starter" : string.Empty
    };

    // Writing and reading the item's local

    [Test]
    public void TheRememberedColorsSurviveThePickUpAndThePlacing()
    {
        var remembered = new List<string> { "lyase_red", "hydrolase_blue", "isomerase_yellow" };

        var written = EnzymeCultureRules.EncodeStarters(EnzymeCultureRules.GetCarriedStarters(remembered, null, Started));
        var restored = EnzymeCultureRules.DecodeStarters(written);

        restored.Should().BeEquivalentTo(remembered);
        foreach (var resref in remembered)
        {
            var (type, color) = EnzymeCultureRules.GetAll().Single(enzyme => EnzymeCultureRules.GetResref(enzyme.Type, enzyme.Color) == resref);
            EnzymeCultureRules.HasStarter(restored, type, color).Should().BeTrue(resref, "the placed vat copies it with no starter");
        }

        EnzymeCultureRules.HasStarter(restored, EnzymeType.Lyase, EnzymeColorType.Black).Should().BeFalse("and still needs one for a color it never copied");
    }

    [Test]
    public void AllFifteenColorsRoundTrip_InTheVatsOwnOrder()
    {
        var every = EnzymeCultureRules.GetAll().Select(enzyme => EnzymeCultureRules.GetResref(enzyme.Type, enzyme.Color)).ToList();

        var text = EnzymeCultureRules.EncodeStarters(every.AsEnumerable().Reverse());

        EnzymeCultureRules.DecodeStarters(text).Should().Equal(every);
        text.Should().Be(string.Join(",", every));
        text.Length.Should().BeLessThan(250, "the item local is a short string");
    }

    [Test]
    public void EveryColorAloneRoundTrips()
    {
        foreach (var (type, color) in EnzymeCultureRules.GetAll())
        {
            var resref = EnzymeCultureRules.GetResref(type, color);

            EnzymeCultureRules.DecodeStarters(EnzymeCultureRules.EncodeStarters(new[] { resref })).Should().Equal(resref);
        }
    }

    [Test]
    public void AVatThatRemembersNothingWritesNothing_AndReadsBackNothing()
    {
        EnzymeCultureRules.EncodeStarters(null).Should().BeEmpty();
        EnzymeCultureRules.EncodeStarters(new List<string>()).Should().BeEmpty();
        EnzymeCultureRules.DecodeStarters(string.Empty).Should().BeEmpty();
        EnzymeCultureRules.DecodeStarters(null).Should().BeEmpty();
        EnzymeCultureRules.DecodeStarters("   ").Should().BeEmpty("an item that never held the local reads as no memory");
    }

    [Test]
    public void TheTextIsReadSafely_UnknownEntriesRepeatsCaseAndSpacesAreHandled()
    {
        EnzymeCultureRules.DecodeStarters("hydrolase_blue,hydrolase_blue,,not_an_enzyme, LYASE_BLACK ,hydrolase_black")
            .Should().Equal("hydrolase_blue", "lyase_black");
        EnzymeCultureRules.EncodeStarters(new[] { "lyase_green", "lyase_green", "wood", "LYASE_BLUE" }).Should().Be("lyase_green,lyase_blue");
    }

    [Test]
    public void TheMemoryIsAnItemLocalOfOneName_ReadWhereItIsWritten()
    {
        FermentationVats.StartersVariable.Should().Be("VAT_STARTERS");
        FermentationVats.StartersVariable.Length.Should().BeLessThanOrEqualTo(32);
        FermentationVats.StartersVariable.Should().NotBe(FermentationVats.CulturedVariable);
    }

    // A running batch's starter goes back to its owner, so the vat does not keep its color

    [Test]
    public void AVatPickedUpMidBatchOnAStarterForgetsThatColor_BecauseTheStarterIsReturned()
    {
        var remembered = new List<string> { "hydrolase_blue", "lyase_red" };
        var job = RunningJob(EnzymeType.Hydrolase, EnzymeColorType.Blue, withStarter: true);

        var carried = EnzymeCultureRules.GetCarriedStarters(remembered, job, Started.AddHours(10));

        carried.Should().Equal(new[] { "lyase_red" }, "the blue starter goes back to the owner with the ingredients");
        remembered.Should().Equal("hydrolase_blue", "lyase_red");
        EnzymeCultureRules.DecodeStarters(EnzymeCultureRules.EncodeStarters(carried)).Should().Equal("lyase_red");
    }

    [Test]
    public void AFinishedBatchsColorIsKept_ItsEnzymeIsDeliveredAndItsStarterWasSpent()
    {
        var job = RunningJob(EnzymeType.Hydrolase, EnzymeColorType.Blue, withStarter: true);

        EnzymeCultureRules.GetCarriedStarters(new[] { "hydrolase_blue" }, job, Started.AddHours(48)).Should().Equal("hydrolase_blue");
        EnzymeCultureRules.GetCarriedStarters(new[] { "hydrolase_blue" }, job, Started.AddDays(9)).Should().Equal("hydrolase_blue");
    }

    [Test]
    public void ARunningBatchOnAColorTheVatAlreadyHadKeepsIt_NoStarterWasUsed()
    {
        var job = RunningJob(EnzymeType.Hydrolase, EnzymeColorType.Blue, withStarter: false);

        EnzymeCultureRules.GetCarriedStarters(new[] { "hydrolase_blue", "lyase_red" }, job, Started.AddHours(10))
            .Should().Equal("hydrolase_blue", "lyase_red");
    }

    [Test]
    public void AVatWithNoBatchCarriesEverything()
    {
        EnzymeCultureRules.GetCarriedStarters(new[] { "hydrolase_blue", "lyase_red" }, null, Started).Should().Equal("hydrolase_blue", "lyase_red");
        EnzymeCultureRules.GetCarriedStarters(null, null, Started).Should().BeEmpty();
        EnzymeCultureRules.GetCarriedStarters(null, RunningJob(EnzymeType.Lyase, EnzymeColorType.Red, true), Started).Should().BeEmpty();
    }

    [Test]
    public void ARunningBatchCannotTeachTwoVatsFromOneStarter()
    {
        // Start the first batch of a color, pick the vat up, place it, and the starter that came back is the only way to that color.
        var vat = new List<string>();
        var starter = EnzymeCultureRules.CanBeStarter("hydrolase_blue", false, EnzymeType.Hydrolase, EnzymeColorType.Blue);
        starter.Should().BeTrue();
        vat = EnzymeCultureRules.WithStarter(vat, EnzymeType.Hydrolase, EnzymeColorType.Blue);
        var job = RunningJob(EnzymeType.Hydrolase, EnzymeColorType.Blue, withStarter: true);

        var placedAgain = EnzymeCultureRules.DecodeStarters(EnzymeCultureRules.EncodeStarters(EnzymeCultureRules.GetCarriedStarters(vat, job, Started.AddHours(1))));

        EnzymeCultureRules.HasStarter(placedAgain, EnzymeType.Hydrolase, EnzymeColorType.Blue).Should().BeFalse("it needs the returned starter again");
    }

    // The wiring: written when picked up, read when placed, before the item is handed over

    [Test]
    public void PickingTheVatUpWritesTheMemoryBeforeTheItemIsHandedOver()
    {
        var source = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Feature", "GuiDefinition", "ViewModel", "ManageStructuresViewModel.cs"));
        var deserialize = source.IndexOf("ObjectPlugin.Deserialize(structure.SerializedItem)", StringComparison.Ordinal);
        var write = source.IndexOf("FermentationVats.WriteStarters(structure.Id, item)", StringComparison.Ordinal);
        var acquire = source.IndexOf("Item.TryAcquireItem(Player, item)", deserialize, StringComparison.Ordinal);
        var deleted = source.IndexOf("Property.DeleteProperty(structure)", deserialize, StringComparison.Ordinal);

        deserialize.Should().BeGreaterThan(0);
        write.Should().BeGreaterThan(deserialize);
        acquire.Should().BeGreaterThan(write, "the local is on the item before the player has it");
        deleted.Should().BeGreaterThan(write, "and the vat's record is read before the structure and its record are deleted");
        source.Substring(write - 120, 120).Should().Contain("structure.StructureType == StructureType.FermentationVat");
    }

    [Test]
    public void PlacingTheVatRestoresTheMemoryFromTheItemTheStructureKeeps()
    {
        var source = File.ReadAllText(Path.Combine(Root, "SWLOR.Game.Server", "Service", "FermentationVats.cs"));
        var spawned = source.IndexOf("public static void OnVatSpawned(", StringComparison.Ordinal);
        var restore = source.IndexOf("RestoreStarters(vat);", spawned, StringComparison.Ordinal);
        var body = source.IndexOf("private static void RestoreStarters(", StringComparison.Ordinal);

        spawned.Should().BeGreaterThan(0);
        restore.Should().BeGreaterThan(spawned, "a placed vat takes its memory as it spawns");
        body.Should().BeGreaterThan(restore);
        var restoreBody = source.Substring(body, 1400);
        restoreBody.Should().Contain("DB.Exists<FermentationVat>", "a vat with a record is left alone, so loading a house never reads the item again");
        restoreBody.Should().Contain("vat.SerializedItem").And.Contain("GetLocalString(item, StartersVariable)").And.Contain("EnzymeCultureRules.DecodeStarters");
        restoreBody.Should().Contain("DestroyObject(item)", "the copy read is not left lying in the world");
    }
}
