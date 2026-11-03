using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TransferService;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Every place the city revamp moves value between a character's inventory and a database record goes through the transfer journal or
/// the export-first checkpoint, in the order that survives a restart (Player City Revamp, farming and the Civic Exchange). These read
/// the sources, as the other wiring tests do, since the services call the engine.
/// </summary>
public class TransferWiringTests
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

    #region The journal

    [Test]
    public void TheProcess_OrdersEachDirection_SoARestartIsSettledFromTheThreePlaces()
    {
        var process = ReadServerSource("Service", "TransferService", "TransferJournalProcess.cs");

        var deposit = ExtractMethod(process, "public static TransferSettlement Deposit(");
        ShouldComeBefore(deposit, "store.SavePending(entry)", "store.TakeAndMark(entry)");
        ShouldComeBefore(deposit, "store.TakeAndMark(entry)", "Settle(store, entry, marked: true)");

        var withdraw = ExtractMethod(process, "public static TransferSettlement Withdraw(");
        ShouldComeBefore(withdraw, "store.SavePending(entry)", "store.Apply(entry)");
        ShouldComeBefore(withdraw, "store.Apply(entry)", "store.MarkApplied(entry)");
        ShouldComeBefore(withdraw, "store.MarkApplied(entry)", "store.GiveAndMark(entry)");
        ShouldComeBefore(withdraw, "store.GiveAndMark(entry)", "store.DeletePending(entry.Id)");
        ShouldComeBefore(withdraw, "store.DeletePending(entry.Id)", "store.ClearMark(entry.Id)");

        var settle = ExtractMethod(process, "private static TransferSettlement Settle(");
        var apply = settle[settle.IndexOf("case TransferSettlement.Apply:", StringComparison.Ordinal)..];
        ShouldComeBefore(apply, "store.Apply(entry)", "store.MarkApplied(entry)");
        ShouldComeBefore(apply, "store.MarkApplied(entry)", "store.DeletePending(entry.Id)");
        ShouldComeBefore(apply, "store.DeletePending(entry.Id)", "store.ClearMark(entry.Id)");
        settle.Should().Contain("entry.AppliedUtc.HasValue || store.IsApplied(entry)", "an applied entry needs no look at a target that may be gone");
    }

    [Test]
    public void TheCharacterIsSavedThroughTheCheckpoint_NotTheQueuedExport()
    {
        var service = ReadServerSource("Service", "TransferJournal.cs");
        service.Should().NotContain("ExportSingleCharacter", "the export command queues a later save and cannot order the file before the database");

        var checkpoint = ReadServerSource("Service", "CharacterCheckpoint.cs");
        checkpoint.Should().Contain("SaveServerCharacter()");

        var take = ExtractMethod(service, "public bool TakeAndMark(TransferEntry entry)");
        ShouldComeBefore(take, "FarmItems.Take(_player", "ObjectPlugin.SetString(_player, MarkVariable");
        ShouldComeBefore(take, "ObjectPlugin.SetString(_player, MarkVariable", "CharacterCheckpoint.Save(_player)");
        take.Should().Contain("entry.Id), true)", "the mark names the entry and is saved in the character file");
        take.Should().NotContain("DB.Set", "the target and the entry are not this step's");
        ShouldComeBefore(take, "if (FarmItems.Count(_player", "FarmItems.Take(_player");

        var give = ExtractMethod(service, "public bool GiveAndMark(TransferEntry entry)");
        ShouldComeBefore(give, "WriteOverflow(entry, index, item.Count - placed)", "ObjectPlugin.SetString(_player, MarkVariable");
        ShouldComeBefore(give, "FarmItems.GiveToInventory(", "ObjectPlugin.SetString(_player, MarkVariable");
        ShouldComeBefore(give, "ObjectPlugin.SetString(_player, MarkVariable", "CharacterCheckpoint.Save(_player)");
        give.Should().NotContain("DB.Set", "the target is changed before the items are given");

        var apply = ExtractMethod(service, "public void Apply(TransferEntry entry)");
        apply.Should().Contain("GetHandler(entry.Kind).Apply(entry)");
        ExtractMethod(service, "public void MarkApplied(TransferEntry entry)").Should().Contain("entry.AppliedUtc = DateTime.UtcNow").And.Contain("DB.Set(entry)");
        ExtractMethod(service, "public void ToOverflow(TransferEntry entry)").Should().Contain("WriteOverflow(entry, index, item.Count)");
        ExtractMethod(service, "private static void WriteOverflow(").Should().Contain("TransferJournalRules.GetOverflowRecordId(entry.Id, index, chunk)");
    }

    [Test]
    public void TheLoginSettlesWhatARestartLeft_ForPlayersOnly()
    {
        var service = ReadServerSource("Service", "TransferJournal.cs");
        service.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]");

        var login = ExtractMethod(service, "public static void SettleOnLogin()");
        login.Should().Contain("TransferJournalProcess.SettleAtLogin(");
        login.Should().Contain("ObjectPlugin.GetString(player, MarkVariable)");
        login.Should().Contain("GetIsDM(player)", "a DM holds no transfer");
        login.Should().Contain("catch (Exception ex)", "one bad entry cannot stop the login");
    }

    [Test]
    public void EveryHandler_IsRegisteredOnce_UnderAUniqueKind()
    {
        var handlers = typeof(TransferJournal).Assembly.GetTypes()
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(field => field.FieldType == typeof(TransferHandler))
            .ToList();

        handlers.Should().HaveCount(10, "one handler per kind of transfer the revamp makes");
        var kinds = handlers.Select(field => ((TransferHandler)field.GetValue(null)!).Kind).ToList();
        kinds.Should().OnlyHaveUniqueItems().And.NotContain(string.Empty);

        var registry = ReadServerSource("Service", "TransferJournal.cs");
        foreach (var field in handlers)
        {
            registry.Should().Contain($"{field.DeclaringType!.Name}.{field.Name}", $"{field.Name} is registered with the journal");
        }

        foreach (var field in handlers)
        {
            var handler = (TransferHandler)field.GetValue(null)!;
            handler.IsApplied.Should().NotBeNull();
            handler.Apply.Should().NotBeNull();
        }
    }

    #endregion

    #region Farmhands

    [Test]
    public void LoadingFeed_IsADepositThroughTheJournal_AndTheHopperIsCreditedWithAReceipt()
    {
        var farmhands = ReadServerSource("Service", "Farmhands.cs");

        var load = ExtractMethod(farmhands, "public static FarmhandRefusal LoadFeed(");
        load.Should().Contain("TransferJournal.Deposit(player, entry)");
        load.Should().Contain("TransferDirection.Deposit");
        load.Should().NotContain("FarmItems.Take(", "the journal takes the crops");
        load.Should().NotContain("DB.Set(farmhand)", "the hopper is credited when the journal applies the entry");
        load.Should().Contain("FarmhandRefusal.NotSaved");

        var apply = ExtractMethod(farmhands, "private static void ApplyFeedLoad(TransferEntry entry)");
        ShouldComeBefore(apply, "FarmhandRules.LoadFeed(", "TransferReceipts.Add(farmhand.TransferReceipts, entry.Id)");
        ShouldComeBefore(apply, "TransferReceipts.Add(farmhand.TransferReceipts, entry.Id)", "DB.Set(farmhand)");
        apply.Split("DB.Set(").Length.Should().Be(2, "the credit and its receipt are one write");
        apply.Should().Contain("TransferReceipts.Has(farmhand.TransferReceipts, entry.Id)", "applying twice credits once");

        ExtractMethod(farmhands, "private static bool CanTakeFeed(TransferEntry entry)")
            .Should().Contain("farmhand.OwnerPlayerId == entry.PlayerId").And.Contain("FarmhandRules.GetFeedRoom(");
    }

    [Test]
    public void TakingFeedBack_AndEmptyingTheOutput_AreWithdrawalsThroughTheJournal()
    {
        var farmhands = ReadServerSource("Service", "Farmhands.cs");

        foreach (var signature in new[] { "public static FarmhandRefusal TakeFeedBack(", "public static FarmhandRefusal EmptyOutput(" })
        {
            var method = ExtractMethod(farmhands, signature);
            method.Should().Contain("Unload(player, farmhand");
            method.Should().NotContain("DB.Set(", "the journal clears the hopper");
            method.Should().NotContain("GiveHoppers", "the hoppers are no longer given before the character is saved");
        }

        var unload = ExtractMethod(farmhands, "private static void Unload(");
        unload.Should().Contain("TransferJournal.Withdraw(player, entry)").And.Contain("TransferDirection.Withdraw");

        var apply = ExtractMethod(farmhands, "private static void ApplyHopperUnload(TransferEntry entry)");
        ShouldComeBefore(apply, "TransferReceipts.Add(farmhand.TransferReceipts, entry.Id)", "DB.Set(farmhand)");
        apply.Split("DB.Set(").Length.Should().Be(2, "the hopper's change and its receipt are one write");
    }

    [Test]
    public void ReleasingAFarmhand_GivesTheOnlineOwnerTheHoppersThroughTheJournal_AndTheOfflineOwnerStableOverflowRecords()
    {
        var farmhands = ReadServerSource("Service", "Farmhands.cs");

        var release = ExtractMethod(farmhands, "private static void Release(Farmhand farmhand, uint ownerIfOn)");
        ShouldComeBefore(release, "TransferJournal.Withdraw(ownerIfOn, entry)", "CityProjects.WriteOverflow(");
        release.Should().Contain("farmhand-hopper:", "the offline records keep their ids");

        var offline = release[release.IndexOf("var recordPrefix", StringComparison.Ordinal)..];
        ShouldComeBefore(offline, "CityProjects.WriteOverflow(", "DB.Delete<Farmhand>(farmhand.Id)");

        farmhands.Should().NotContain("GiveHoppers", "no path gives the hoppers before the farmhand is released");
        ExtractMethod(farmhands, "private static void ApplyRelease(TransferEntry entry)").Should().Contain("DB.Delete<Farmhand>(entry.TargetId)");
    }

    #endregion

    #region Livestock

    [Test]
    public void PlacingAnAnimal_IsADepositThroughTheJournal_WithTheCrateSavedAsItIs()
    {
        var corrals = ReadServerSource("Service", "Corrals.cs");

        var place = ExtractMethod(corrals, "public static LivestockResult Place(");
        place.Should().Contain("TransferJournal.Deposit(player, entry, crate != null ? new[] { item } : new uint[0])");
        place.Should().Contain("TransferJournalRules.Held(GetResRef(item), GetName(item), ObjectPlugin.Serialize(item))", "a crate carries the animal's timers");
        place.Should().NotContain("DB.Set(animal)", "the animal is recorded when the journal applies the entry");
        place.Should().NotContain("UseUpOne", "the journal takes the item");
        ShouldComeBefore(place, "LivestockRules.Unpack(", "TransferJournal.Deposit(");

        var apply = ExtractMethod(corrals, "private static void ApplyPlacement(TransferEntry entry)");
        ShouldComeBefore(apply, "DB.Set(animal)", "SpawnCreature(animal, placeable)");
        apply.Should().Contain("GetAnimal(animal.Id) != null", "applying twice records one animal");

        ExtractMethod(corrals, "private static bool CanTakeAnimal(TransferEntry entry)")
            .Should().Contain("LivestockRules.MaxAnimalsPerCdKey").And.Contain("LivestockRules.AnimalsPerCorral").And.Contain("GetCorral(animal.CorralId)");
    }

    [Test]
    public void FeedingAnAnimal_TakesTheCropsAndSavesTheCharacter_BeforeTheMealIsRecorded()
    {
        var feed = ExtractMethod(ReadServerSource("Service", "Corrals.cs"), "public static LivestockResult Feed(");

        ShouldComeBefore(feed, "TransferJournal.TakeAndSave(player, (crop.HarvestResref, LivestockRules.FeedCrops))", "LivestockRules.ApplyFeeding(animal, now)");
        ShouldComeBefore(feed, "LivestockRules.ApplyFeeding(animal, now)", "DB.Set(animal)");
        feed.Should().NotContain("FarmItems.Take(");
        feed.Should().Contain("LivestockRefusal.NotSaved");
    }

    [Test]
    public void CollectingButcheringAndCrating_AreWithdrawalsThroughTheJournal()
    {
        var corrals = ReadServerSource("Service", "Corrals.cs");

        var collect = ExtractMethod(corrals, "public static LivestockResult Collect(");
        collect.Should().Contain("GiveProducts(player, animal, definition, items, \"collected from\", now, CollectKind)");
        collect.Should().NotContain("DB.Set(animal)", "the journal records the collection");

        var butcher = ExtractMethod(corrals, "public static LivestockResult Butcher(");
        butcher.Should().Contain("GiveProducts(player, animal, definition, items, \"butchered\", now, RemoveAnimalKind)");
        butcher.Should().NotContain("DB.Delete<Livestock>", "the journal deletes the animal");

        var products = ExtractMethod(corrals, "private static LivestockResult GiveProducts(");
        products.Should().Contain("TransferJournal.Withdraw(player, entry, out var overflowByItem)");
        products.Should().NotContain("FarmItems.Give(");

        var crate = ExtractMethod(corrals, "public static LivestockResult Crate(");
        crate.Should().Contain("TransferJournal.Withdraw(player, entry, out var itemsInOverflow)");
        crate.Should().Contain("TransferJournalRules.Held(LivestockRules.CrateResref, GetName(item), ObjectPlugin.Serialize(item))");
        crate.Should().NotContain("DB.Delete<Livestock>", "the journal deletes the animal");
        crate.Should().NotContain("Item.TryAcquireItem(", "the journal gives the crate");
        ShouldComeBefore(crate, "CrateThroughClerk(player, animal, definition, crate, now)", "TransferJournal.Withdraw(");

        var collectApply = ExtractMethod(corrals, "private static void ApplyCollectionTransfer(TransferEntry entry)");
        collectApply.Should().Contain("LivestockRules.ApplyCollection(animal, entry.CreatedUtc)");
        ExtractMethod(corrals, "private static bool IsCollected(TransferEntry entry)").Should().Contain("animal.LastCollectedUtc.Value >= entry.CreatedUtc");
        ExtractMethod(corrals, "private static void ApplyRemoval(TransferEntry entry)").Should().Contain("DB.Delete<Livestock>(entry.TargetId)");
    }

    #endregion

    #region Farming

    [Test]
    public void Planting_TakesTheSeedAndSavesTheCharacter_BeforeTheCropIsRecorded()
    {
        var plant = ExtractMethod(ReadServerSource("Service", "Farming.cs"), "public static FarmingResult Plant(");

        ShouldComeBefore(plant, "TransferJournal.TakeAndSave(player, consumed)", "DB.Set(new GardenPlot");
        plant.Should().Contain("FarmingRefusal.NotSaved");
        plant.Should().NotContain("UseUpOne");
    }

    [Test]
    public void HarvestingAndLosingACrop_AreWithdrawalsThroughTheJournal()
    {
        var farming = ReadServerSource("Service", "Farming.cs");

        var harvest = ExtractMethod(farming, "public static FarmingResult Harvest(");
        ShouldComeBefore(harvest, "ClerkRecovery.RunCrop(", "TransferJournal.Withdraw(player, entry, out var overflowByItem)");
        harvest.Should().Contain("TransferJournalRules.Plain(crop.HarvestResref");
        harvest.Should().NotContain("DB.Delete<GardenPlot>", "the journal deletes the crop");
        harvest.Should().NotContain("GiveItems(");

        var retrieved = ExtractMethod(farming, "public static void OnPlotRetrieved(");
        ShouldComeBefore(retrieved, "TransferJournal.Withdraw(planter, entry, out var overflowByItem)", "CityProjects.WriteOverflow(");
        var offline = retrieved[retrieved.IndexOf("CityProjects.WriteOverflow(", StringComparison.Ordinal)..];
        ShouldComeBefore(offline, "CityProjects.WriteOverflow(", "DB.Delete<GardenPlot>(record.Id)");
        retrieved.Should().Contain("garden-seed:{record.Id}:{record.PlantedUtc.Ticks}", "the offline planter's record keeps its id");

        var removed = ExtractMethod(farming, "private static bool IsCropRemoved(TransferEntry entry)");
        removed.Should().Contain("record.PlantedUtc.Ticks.ToString() != ", "a crop planted later is not the one the entry removes");
        ExtractMethod(farming, "private static void ApplyCropRemoval(TransferEntry entry)").Should().Contain("DB.Delete<GardenPlot>(entry.TargetId)");
    }

    #endregion

    #region Fermentation

    [Test]
    public void StartingABatch_IsADepositThroughTheJournal_WithTheStarterSavedAsItIs()
    {
        var vats = ReadServerSource("Service", "FermentationVats.cs");

        var start = ExtractMethod(vats, "public static FermentationResult Start(");
        start.Should().Contain("TransferJournal.Deposit(player, entry, needsStarter ? new[] { starter } : new uint[0])");
        start.Should().Contain("TransferJournalRules.Held(GetResRef(starter), GetName(starter), starterData)");
        start.Should().NotContain("FarmItems.Take(").And.NotContain("DestroyObject(starter)").And.NotContain("DB.Set(");
        start.Should().Contain("FermentationRefusal.NotSaved");

        var apply = ExtractMethod(vats, "private static void ApplyStart(TransferEntry entry)");
        ShouldComeBefore(apply, "DB.Set(record)", "DB.Set(job)");
        apply.Should().Contain("IsStarted(entry)", "applying twice starts one batch");

        ExtractMethod(vats, "private static bool IsStarted(TransferEntry entry)").Should().Contain("job.StartedUtc == entry.CreatedUtc");
        ExtractMethod(vats, "private static bool CanTakeBatch(TransferEntry entry)")
            .Should().Contain("GetVatStructure(job.VatId) != null").And.Contain("GetJob(job.VatId) == null").And.Contain("MaxRunningPerCdKey");
    }

    [Test]
    public void CollectingABatch_IsAWithdrawalThroughTheJournal_WithTheEnzymeSavedAsRolled()
    {
        var vats = ReadServerSource("Service", "FermentationVats.cs");

        var collect = ExtractMethod(vats, "public static FermentationResult Collect(");
        ShouldComeBefore(collect, "EnzymeRolls.Roll(", "TransferJournal.Withdraw(player, entry, out var overflowByItem)");
        collect.Should().Contain("TransferJournalRules.Held(GetResRef(item), GetName(item), ObjectPlugin.Serialize(item))");
        collect.Should().NotContain("DB.Delete<FermentationJob>", "the journal deletes the batch");
        collect.Should().NotContain("Item.TryAcquireItem(", "the journal gives the enzyme");

        ExtractMethod(vats, "private static bool IsCollected(TransferEntry entry)").Should().Contain("job.StartedUtc.Ticks.ToString() != ");
        ExtractMethod(vats, "private static void ApplyCollection(TransferEntry entry)").Should().Contain("DB.Delete<FermentationJob>(entry.TargetId)");
    }

    #endregion

    #region Civic Exchange and stores

    [Test]
    public void ACivicExchangeCrate_IsPaidAndGivenInOneJournaledWithdrawal()
    {
        var exchange = ReadServerSource("Service", "CivicExchange.cs");

        var crate = ExtractMethod(exchange, "private static string BuyCrate(");
        crate.Should().Contain("TransferJournal.Withdraw(player, entry, out var overflowByItem)");
        crate.Should().NotContain("UnallocatedXP", "the journal's handler takes the XP");
        crate.Should().NotContain("DB.Set(", "the journal's handler saves the XP and the allowance");
        crate.Should().NotContain("FarmItems.Give(").And.NotContain("ExportSingleCharacter");

        var apply = ExtractMethod(exchange, "private static void ApplyCrate(TransferEntry entry)");
        ShouldComeBefore(apply, "dbPlayer.UnallocatedXP -= ", "TransferReceipts.Add(dbPlayer.TransferReceipts, entry.Id)");
        ShouldComeBefore(apply, "TransferReceipts.Add(dbPlayer.TransferReceipts, entry.Id)", "DB.Set(dbPlayer)");
        ShouldComeBefore(apply, "DB.Set(dbPlayer)", "CivicExchangeRules.RecordCrate(allowance");
        apply.Should().Contain("TransferReceipts.Has(dbPlayer.TransferReceipts, entry.Id)", "applying twice charges once");
    }

    [Test]
    public void ASalesTaxAndAnAllowance_AreRecordedOnlyAfterTheBuyersCharacterIsSaved()
    {
        var after = ExtractMethod(ReadServerSource("Service", "CityStores.cs"), "public static void OnBuyAfter()");

        ShouldComeBefore(after, "CharacterCheckpoint.Save(buyer)", "CreditTax(cityId, tax, buyer)");
        ShouldComeBefore(after, "CharacterCheckpoint.Save(buyer)", "CityStockAllowances.RecordTravelerSale(");
        ShouldComeBefore(after, "CharacterCheckpoint.Save(buyer)", "CityStockAllowances.RecordSale(");
    }

    #endregion

    [Test]
    public void TheJournalFiles_AreAsciiOnly()
    {
        foreach (var path in new[]
                 {
                     new[] { "Service", "TransferJournal.cs" },
                     new[] { "Service", "CharacterCheckpoint.cs" },
                     new[] { "Service", "TransferService", "TransferJournalRules.cs" },
                     new[] { "Service", "TransferService", "TransferJournalProcess.cs" },
                     new[] { "Entity", "TransferEntry.cs" }
                 })
        {
            ReadServerSource(path).Where(c => c > 0x7E).Should().BeEmpty("player-facing punctuation is ASCII");
        }
    }
}
