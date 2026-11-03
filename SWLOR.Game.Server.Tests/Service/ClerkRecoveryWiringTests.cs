using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.LivestockService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Pins the wiring of the City Hall clerk's recovery (Player City Revamp, "Recovery"): where each hook calls the rules, the order of the
/// steps that write to overflow storage, the places that finish an interrupted action before they delete anything, and the mayor's
/// power check. The engine-bound services cannot run in a unit test, so the rules and the order of the steps are tested in
/// <see cref="ClerkRecoveryRulesTests"/> and <see cref="ClerkRecoveryProcessTests"/>, and this class checks the code that feeds them.
/// </summary>
public class ClerkRecoveryWiringTests
{
    private static string ReadServerSource(params string[] path)
    {
        var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "SWLOR.Game.Server.sln")))
            root = root.Parent;

        root.Should().NotBeNull();
        return File.ReadAllText(Path.Combine(new[] { root!.FullName, "SWLOR.Game.Server" }.Concat(path).ToArray()));
    }

    private static string ExtractMethod(string source, string signature)
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

    private static void ShouldComeBefore(string method, string first, string second)
    {
        var a = method.IndexOf(first, StringComparison.Ordinal);
        var b = method.IndexOf(second, StringComparison.Ordinal);
        a.Should().BeGreaterThanOrEqualTo(0, $"'{first}' should be there");
        b.Should().BeGreaterThanOrEqualTo(0, $"'{second}' should be there");
        a.Should().BeLessThan(b, $"'{first}' should come before '{second}'");
    }

    #region The records

    [Test]
    public void ACropAndAnAnimal_CarryTheMark_AndItSurvivesTheDatabasesJson()
    {
        var mark = ClerkRecoveryRules.BuildCrateMark(
            "mark-1",
            "owner",
            new DateTime(2026, 11, 2, 12, 0, 0, DateTimeKind.Utc),
            new LivestockCrate { Species = LivestockType.Thune, Feedings = 3, MaturitySeconds = 60, HungerSeconds = 7200, CollectSeconds = 30, PenaltySeconds = 10 });
        mark.Items["manure_t5"] = 1;

        var animal = JsonConvert.DeserializeObject<Livestock>(JsonConvert.SerializeObject(new Livestock { Recovery = mark }))!;
        var plot = JsonConvert.DeserializeObject<GardenPlot>(JsonConvert.SerializeObject(new GardenPlot { Recovery = mark }))!;

        foreach (var read in new[] { animal.Recovery, plot.Recovery })
        {
            read.Should().NotBeNull();
            read!.Id.Should().Be("mark-1");
            read.Action.Should().Be(ClerkRecoveryAction.Crate);
            read.PlayerId.Should().Be("owner");
            read.AtUtc.Should().Be(mark.AtUtc);
            read.Items.Should().Equal(new Dictionary<string, int> { ["manure_t5"] = 1 });
            read.Crate.Should().BeEquivalentTo(mark.Crate);
        }
    }

    [Test]
    public void ARecordSavedBeforeTheMarkExisted_LoadsWithNone()
    {
        JsonConvert.DeserializeObject<Livestock>("{\"Species\":1}")!.Recovery.Should().BeNull();
        JsonConvert.DeserializeObject<GardenPlot>("{\"PlotId\":\"p\"}")!.Recovery.Should().BeNull();
        JsonConvert.DeserializeObject<Player>("{\"Name\":\"x\"}")!.CityDepartures.Should().NotBeNull().And.BeEmpty();
    }

    #endregion

    #region The steps

    [Test]
    public void ARemoteHarvest_MarksTheCropAndWritesOverflow_BeforeTheOrdinaryDeleteOfTheRecord()
    {
        var harvest = ExtractMethod(ReadServerSource("Service", "Farming.cs"), "public static FarmingResult Harvest(");

        harvest.Should().Contain("if (toOverflow)");
        ShouldComeBefore(harvest, "if (toOverflow)", "DB.Delete<GardenPlot>(record.Id)");
        ShouldComeBefore(harvest, "ClerkRecovery.RunCrop(", "DB.Delete<GardenPlot>(record.Id)");
        var remote = harvest[harvest.IndexOf("if (toOverflow)", StringComparison.Ordinal)..harvest.IndexOf("DB.Delete<GardenPlot>(record.Id)", StringComparison.Ordinal)];
        remote.Should().Contain("return FarmingResult.Success(");
        remote.Should().NotContain("GiveItems", "a remote harvest never puts anything in an inventory");
        remote.Should().NotContain("DB.Delete", "the store deletes the crop, after the overflow records");
    }

    [Test]
    public void TheCropStore_DeletesTheCropOnlyWhenTheActionFinishes()
    {
        var store = ReadServerSource("Service", "CropRecoveryStore.cs");

        ExtractMethod(store, "public void FinishSource(").Should().Contain("DB.Delete<GardenPlot>(_recordId)");
        ExtractMethod(store, "public void WriteMark(").Should().Contain("record.Recovery = mark").And.Contain("DB.Set(record)");
        store.Split("DB.Delete<GardenPlot>").Length.Should().Be(2, "only the finish deletes the crop");
    }

    [TestCase("public static LivestockResult Collect(", "ClerkRecoveryAction.Collect", "LivestockRules.ApplyCollection(animal, now)")]
    [TestCase("public static LivestockResult Butcher(", "ClerkRecoveryAction.Butcher", "DB.Delete<Livestock>(animal.Id)")]
    public void ARemoteCollectionOrButchering_GoesThroughTheMark_BeforeTheOrdinaryChangeToTheAnimal(string signature, string action, string ordinaryChange)
    {
        var method = ExtractMethod(ReadServerSource("Service", "Corrals.cs"), signature);

        method.Should().Contain("if (toOverflow)").And.Contain("GiveProductsThroughClerk(").And.Contain(action);
        ShouldComeBefore(method, "if (toOverflow)", ordinaryChange);
        ShouldComeBefore(method, "GiveProductsThroughClerk(", ordinaryChange);
    }

    [Test]
    public void ARemoteCrate_GoesThroughTheMark_BeforeTheOrdinaryCrateIsMade()
    {
        var method = ExtractMethod(ReadServerSource("Service", "Corrals.cs"), "public static LivestockResult Crate(");

        ShouldComeBefore(method, "if (toOverflow)", "CreateCrateItem(crate, definition, GetLocation(player))");
        ShouldComeBefore(method, "CrateThroughClerk(", "DB.Delete<Livestock>(animal.Id)");
        method.Should().NotContain("var inOverflow = toOverflow", "the clerk's path no longer shares the ordinary one");
    }

    [Test]
    public void TheClerkProducts_MarkTheAnimal_ThenGiveTheXp_AndNeverTouchAnInventory()
    {
        var method = ExtractMethod(ReadServerSource("Service", "Corrals.cs"), "private static LivestockResult GiveProductsThroughClerk(");

        ShouldComeBefore(method, "ClerkRecovery.RunAnimal(", "GiveProductXp(");
        method.Should().NotContain("FarmItems.Give");
    }

    [Test]
    public void TheCrateIsProbed_BeforeAnAnimalIsMarkedForOne()
    {
        var corrals = ReadServerSource("Service", "Corrals.cs");

        ShouldComeBefore(ExtractMethod(corrals, "private static LivestockResult CrateThroughClerk("), "CanCreateCrate(definition)", "ClerkRecovery.RunAnimal(");
        ShouldComeBefore(ExtractMethod(ReadServerSource("Service", "ClerkRecovery.cs"), "public static ClerkRecoveryResult SendAbsentOwnersAnimalsAway("), "Corrals.CanCreateCrate(definition)", "RunAnimal(");
    }

    [Test]
    public void TheAnimalStore_DeletesAButcheredOrCratedAnimal_AndRecordsACollectionAtTheTimeTheClerkTookIt()
    {
        var finish = ExtractMethod(ReadServerSource("Service", "AnimalRecoveryStore.cs"), "public void FinishSource(");

        finish.Should().Contain("mark.Action != ClerkRecoveryAction.Collect").And.Contain("DB.Delete<Livestock>(_animalId)");
        finish.Should().Contain("LivestockRules.ApplyCollection(animal, mark.AtUtc)").And.Contain("animal.Recovery = null");
    }

    #endregion

    #region Finishing an interrupted action

    [Test]
    public void AnInterruptedHarvest_IsFinishedBeforeAnythingDeletesTheCrop()
    {
        var farming = ReadServerSource("Service", "Farming.cs");

        ExtractMethod(farming, "public static CropType RemoveWitheredCrop(").Should().Contain("ClerkRecovery.RecoverPlot(plotId)");
        ShouldComeBefore(ExtractMethod(farming, "public static CropType RemoveWitheredCrop("), "ClerkRecovery.RecoverPlot(plotId)", "DB.Delete<GardenPlot>(record.Id)");
        ShouldComeBefore(ExtractMethod(farming, "public static FarmingResult Clear("), "ClerkRecovery.RecoverPlot(plotId)", "DB.Delete<GardenPlot>(record.Id)");
        ShouldComeBefore(ExtractMethod(farming, "public static void OnPlotRetrieved("), "ClerkRecovery.RecoverPlot(plot.Id)", "DB.Delete<GardenPlot>(record.Id)");
        ShouldComeBefore(ExtractMethod(farming, "public static void OnPropertyDeleted("), "ClerkRecovery.RecoverPlot(propertyId)", "DB.Delete<GardenPlot>(GardenPlot.KeyFor(propertyId))");
        ShouldComeBefore(ExtractMethod(farming, "public static void ProcessPlotsAtServerStart("), "record.Recovery != null", "DB.Delete<GardenPlot>(record.Id)");
    }

    [Test]
    public void AnInterruptedAnimalAction_IsFinishedBeforeAnythingReplaysOrDeletesTheAnimal()
    {
        var corrals = ReadServerSource("Service", "Corrals.cs");

        ShouldComeBefore(ExtractMethod(corrals, "public static void ProcessAtServerStart("), "ClerkRecovery.RecoverAnimalsAtServerStart()", "Farmhands.CatchUpAll()");
        ShouldComeBefore(ExtractMethod(corrals, "public static Livestock LoadAnimal("), "ClerkRecovery.RecoverAnimal(animalId)", "Farmhands.CatchUp(animal.CorralId)");
        ShouldComeBefore(ExtractMethod(corrals, "public static void RemoveWanderedOff("), "ClerkRecovery.RecoverAnimal(animal.Id)", "DB.Delete<Livestock>(animal.Id)");
        ShouldComeBefore(ExtractMethod(corrals, "private static void SendToOverflow("), "ClerkRecovery.RecoverAnimal(animal.Id)", "DB.Delete<Livestock>(animal.Id)");
    }

    [Test]
    public void AtServerStart_AnErrorOnOneAnimal_DoesNotStopTheRest()
    {
        var method = ExtractMethod(ReadServerSource("Service", "ClerkRecovery.cs"), "public static void RecoverAnimalsAtServerStart()");

        method.Should().Contain("try").And.Contain("catch (Exception ex)").And.Contain("LogGroup.Error");
    }

    #endregion

    #region What the clerk offers

    [Test]
    public void TheOffers_AreOnlyForTheCropsAndAnimalsOfThePlayerInThisCityThatTheyCannotReach()
    {
        var clerk = ReadServerSource("Service", "ClerkRecovery.cs");

        var crop = ExtractMethod(clerk, "private static ClerkRecoveryOffer FindCropOffer(");
        crop.Should().Contain("record.PlanterPlayerId != playerId").And.Contain("Property.GetCityIdForProperty(structure.ParentPropertyId) != cityId");
        crop.Should().Contain("ClerkRecoveryRules.CanReachCrop(Farming.HasGardenPermission(player, structure), IsCitizenOf(playerId, cityId), CanEnterPlotProperty(playerId, parent))");

        var entry = ExtractMethod(clerk, "private static bool CanEnterPlotProperty(");
        entry.Should().Contain("PropertyPermissionType.EnterProperty").And.Contain("parent.IsPubliclyAccessible")
            .And.Contain("ClerkRecoveryRules.CanEnterPlotProperty(");
        crop.Should().Contain("ClerkRecoveryRules.GetCropActions(");

        var animal = ExtractMethod(clerk, "private static ClerkRecoveryOffer FindAnimalOffer(");
        animal.Should().Contain("animal.OwnerPlayerId != playerId").And.Contain("corral.ParentPropertyId != cityId");
        animal.Should().Contain("ClerkRecoveryRules.CanReachAnimal(IsCitizenOf(playerId, cityId))");
        animal.Should().Contain("ClerkRecoveryRules.GetAnimalActions(");
    }

    [Test]
    public void EveryClerkAction_ChecksTheOfferFirst_SoACropOrAnimalThePlayerCanReachIsNeverRecoveredRemotely()
    {
        var clerk = ReadServerSource("Service", "ClerkRecovery.cs");

        ShouldComeBefore(ExtractMethod(clerk, "public static ClerkRecoveryResult Harvest("), "FindCropOffer(", "Farming.Harvest(player, plotId, true)");
        ShouldComeBefore(ExtractMethod(clerk, "public static ClerkRecoveryResult Clear("), "FindCropOffer(", "Farming.Clear(player, plotId)");
        ShouldComeBefore(ExtractMethod(clerk, "private static ClerkRecoveryResult ActOnAnimal("), "FindAnimalOffer(", "action()");

        clerk.Should().Contain("Corrals.Collect(player, animalId, true)");
        clerk.Should().Contain("Corrals.Butcher(player, animalId, true)");
        clerk.Should().Contain("Corrals.Crate(player, animalId, true)");
    }

    [Test]
    public void TheOverflowRecords_UseTheMarksRepeatableIds()
    {
        var clerk = ReadServerSource("Service", "ClerkRecovery.cs");

        ExtractMethod(clerk, "internal static void WriteOverflowItems(").Should().Contain("ClerkRecoveryRules.GetOverflowRecordId(recordPrefix, chunk)").And.Contain("new OverflowItem");
        ExtractMethod(clerk, "internal static void WriteOverflowCrate(").Should().Contain("ClerkRecoveryRules.GetOverflowRecordId(recordPrefix, 0)");
    }

    [Test]
    public void TheClerkConversation_OffersRecoveryToTheClerkOnly()
    {
        var spec = new CityStaffDialog().Build();
        spec.Pages.Keys.Should().BeEquivalentTo("MAIN_PAGE", "RECOVERY_PAGE");

        var dialog = ReadServerSource("Feature", "DialogDefinition", "CityStaffDialog.cs");
        var main = ExtractMethod(dialog, "private void MainPageInit(");
        main.Should().Contain("role == CityStaffRole.Clerk").And.Contain("ClerkRecoveryRules.RecoverResponse");
        ShouldComeBefore(main, "role == CityStaffRole.Clerk", "ClerkRecoveryRules.RecoverResponse");
        dialog.Should().Contain("ClerkRecovery.GetOffers(Player, cityId)");
    }

    [Test]
    public void ClearingAndButchering_AskForConfirmation_BecauseTheyCannotBeUndone()
    {
        var show = ExtractMethod(ReadServerSource("Feature", "DialogDefinition", "CityStaffDialog.cs"), "private void ShowOffer(");

        show.Should().Contain("model.Confirm == ClerkRecoveryAction.Clear").And.Contain("model.Confirm == ClerkRecoveryAction.Butcher");
        ShouldComeBefore(show, "model.Confirm == ClerkRecoveryAction.Clear", "ClerkRecovery.Clear(");
        ShouldComeBefore(show, "model.Confirm == ClerkRecoveryAction.Butcher", "ClerkRecovery.Butcher(");
        show.Should().Contain("page.AddResponse(\"Clear it\", () => model.Confirm = ClerkRecoveryAction.Clear)");
        show.Should().Contain("page.AddResponse(\"Butcher it\", () => model.Confirm = ClerkRecoveryAction.Butcher)");
    }

    #endregion

    #region The mayor

    [Test]
    public void TheMayorsSendAway_ChecksThePower_BeforeAnythingIsMarked_AndNotifiesTheOwnerAfter()
    {
        var send = ExtractMethod(ReadServerSource("Service", "ClerkRecovery.cs"), "public static ClerkRecoveryResult SendAbsentOwnersAnimalsAway(");

        send.Should().Contain("CityPowers.Can(city, GetObjectUUID(player), ClerkRecoveryRules.SendAwayPower)");
        ShouldComeBefore(send, "CityPowers.Can(", "RunAnimal(");
        ShouldComeBefore(send, "GetAbsentOwnersAnimals(corral, now)", "RunAnimal(");
        ShouldComeBefore(send, "RunAnimal(", "Corrals.Notify(");
        send.Should().Contain("ClerkRecoveryRules.BuildCrateMark(");
    }

    [Test]
    public void AnOwnerIsAbsent_OnlyByTheRulesSevenDays_FromTheDepartureRecordedOnTheirCharacter()
    {
        var absent = ExtractMethod(ReadServerSource("Service", "ClerkRecovery.cs"), "private static bool IsOwnerAbsent(");

        absent.Should().Contain("owner.CityDepartures").And.Contain("ClerkRecoveryRules.IsAbsent(isCitizen, isDeleted, left, now)");
    }

    [Test]
    public void TheCorralMenu_OffersTheSendAwayOnlyToWhoHoldsThePower_AndOnlyWhenThereIsSomethingToSend()
    {
        var dialog = ReadServerSource("Feature", "DialogDefinition", "CorralDialog.cs");
        var corral = ExtractMethod(dialog, "private void CorralPageInit(");

        corral.Should().Contain("CityPowers.Can(city, GetObjectUUID(Player), ClerkRecoveryRules.SendAwayPower)");
        ShouldComeBefore(corral, "CityPowers.Can(", "GetAbsentOwnersAnimals(corral, now)");
        corral.Should().Contain("if (absent.Count > 0)");
        ExtractMethod(dialog, "private void SendAwayPageInit(").Should().Contain("ClerkRecovery.SendAbsentOwnersAnimalsAway(Player,");
    }

    [Test]
    public void LeavingACity_RecordsTheDeparture_AndJoiningItAgainEndsIt()
    {
        var source = ReadServerSource("Feature", "GuiDefinition", "ViewModel", "ManageCitizenshipViewModel.cs");

        var leave = source[source.IndexOf("dbPlayer.CitizenPropertyId = string.Empty;", StringComparison.Ordinal)..];
        ShouldComeBefore(leave, "ClerkRecoveryRules.RecordDeparture(dbPlayer.CityDepartures, _cityPropertyId, DateTime.UtcNow)", "DB.Set(dbPlayer)");

        var join = source[source.IndexOf("dbPlayer.CitizenPropertyId = _cityPropertyId;", StringComparison.Ordinal)..];
        ShouldComeBefore(join, "ClerkRecoveryRules.ClearDeparture(dbPlayer.CityDepartures, _cityPropertyId)", "DB.Set(dbPlayer)");
    }

    #endregion
}
