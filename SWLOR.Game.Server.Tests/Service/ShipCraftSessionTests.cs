using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.CraftService.Minigame.Circuit;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The space revamp's crafting hooks on the crafting revamp's sessions: the Shipyard fee and build speed, the component traits a
/// ship module, deed or variant keeps, the rare-trait slot's stand-ins and the blueprint traits. Each must survive every way a craft
/// ends: a manual success, a quick craft, a broken piece, a closed window, a logout and a server restart.
/// </summary>
public class ShipCraftSessionTests
{
    private const RecipeType ShipRecipe = RecipeType.ShipDeedStriker;
    private const uint NoItem = OBJECT_INVALID;
    private const int ShipyardFee = 500;

    private FakeCraftSessionWorld _world;
    private Player _player;
    private RecipeDetail _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = new RecipeDetail
        {
            Resref = "sd_striker",
            Skill = SkillType.Engineering,
            Category = RecipeCategoryType.Starship,
            Level = 5,
            Components = new Dictionary<string, int> { ["sc_hframe_1"] = 1, ["ref_tilarium"] = 3 },
            EnhancementType = RecipeEnhancementType.Starship,
            EnhancementSlots = 1
        };

        _world = new FakeCraftSessionWorld();
        _world.Recipes[ShipRecipe] = _recipe;
        _world.Inventory["sc_hframe_1"] = 1;
        _world.Inventory["ref_tilarium"] = 3;

        _player = new Player("pilot-1");
        _player.Skills[SkillType.Engineering] = new PlayerSkill { Rank = 5 };
    }

    private CraftSessionLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private CraftSession Start(CraftSessionLifecycle lifecycle, uint blueprint = NoItem)
    {
        var result = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, _player, new CraftSessionStartRequest(ShipRecipe, blueprint, new List<uint>()));
        result.Succeeded.Should().BeTrue(result.Message);
        lifecycle.RecordActions(_player, result.Session, CraftSessionTestPace.Actions);
        _world.AdvanceByActions(CraftSessionTestPace.Actions);
        return result.Session;
    }

    private static string[] Committed => new[] { "sc_hframe_1 x1", "ref_tilarium x3" };

    // ---- Shipyard fee ----------------------------------------------------------------------------------------------------

    [Test]
    public void Start_TakesTheShipyardFeeOnce_WithTheComponentsAndTheBlueprint()
    {
        _world.StationFee = ShipyardFee;
        _world.Gold = 2000;

        Start(Lifecycle(), FakeCraftSessionWorld.Blueprint);

        _world.StationFeesPaid.Should().Equal(ShipyardFee);
        _world.Gold.Should().Be(1500);
        _world.Events.Should().ContainInOrder("take-components", "charge-blueprint", "station-fee", "begin-work");
        _player.ActiveCraftSession.Should().NotBeNull();
    }

    [Test]
    public void Start_RefusesAndTakesNothing_WhenTheShipyardFeeCannotBePaid()
    {
        _world.StationFee = ShipyardFee;
        _world.Gold = ShipyardFee - 1;

        var result = Lifecycle().Start(FakeCraftSessionWorld.PlayerHandle, _player, new CraftSessionStartRequest(ShipRecipe, NoItem, new List<uint>()));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be($"The Shipyard fee is {ShipyardFee} credits!");
        _world.Inventory["sc_hframe_1"].Should().Be(1);
        _world.Inventory["ref_tilarium"].Should().Be(3);
        _world.StationFeesPaid.Should().BeEmpty();
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void Start_WithNoStationFee_ChargesNothingAndKeepsTheEventsOfAnyOtherCraft()
    {
        Start(Lifecycle());

        _world.StationFeesPaid.Should().BeEmpty();
        _world.Events.Should().NotContain("station-fee");
    }

    [TestCase(CraftSessionEndReason.Broken)]
    [TestCase(CraftSessionEndReason.WindowClosed)]
    [TestCase(CraftSessionEndReason.Interrupted)]
    public void Fail_DoesNotRefundTheShipyardFee(CraftSessionEndReason reason)
    {
        _world.StationFee = ShipyardFee;
        _world.Gold = 2000;
        Start(Lifecycle(100, 100));

        Lifecycle(100, 100).Fail(FakeCraftSessionWorld.PlayerHandle, _player, reason);

        _world.Gold.Should().Be(1500, "the fee pays for the Shipyard's time, as before the crafting revamp");
        _world.StationFeesPaid.Should().Equal(ShipyardFee);
        _world.Treasury.Should().Be(ShipyardFee, "the station is credited when the craft ends");
        _player.PendingStationFees.Should().BeEmpty();
    }

    // ---- Shipyard fee: the treasury is credited once, when the craft ends ------------------------------------------------

    private CraftSession StartWithFee(CraftSessionLifecycle lifecycle)
    {
        _world.StationFee = ShipyardFee;
        _world.Gold = 2000;
        _world.ExportNow();
        return Start(lifecycle);
    }

    private static Player Copy(Player player) => JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

    [Test]
    public void Start_TakesTheFeeFromThePlayer_ButLeavesTheTreasuryAlone()
    {
        StartWithFee(Lifecycle());

        _world.Gold.Should().Be(1500);
        _world.Treasury.Should().Be(0, "the start's payment is not in a character file yet");
        _player.ActiveCraftSession.StationFee.Should().Be(ShipyardFee);
        _player.ActiveCraftSession.StationFeePropertyId.Should().Be(FakeCraftSessionWorld.StationId);
    }

    [Test]
    public void Success_CreditsTheTreasuryOnce()
    {
        var lifecycle = Lifecycle();
        var session = StartWithFee(lifecycle);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), CraftSessionTestPace.Actions);

        _world.Treasury.Should().Be(ShipyardFee);
        _world.CreditedFees.Should().Equal(session.SessionId);
        _player.PendingStationFees.Should().BeEmpty();
        _world.Gold.Should().Be(1500);
    }

    [TestCase(CraftSessionEndReason.Broken)]
    [TestCase(CraftSessionEndReason.WindowClosed)]
    public void FailureAndClose_CreditTheTreasuryOnce_WithNoRefund(CraftSessionEndReason reason)
    {
        var lifecycle = Lifecycle(100, 100);
        StartWithFee(lifecycle);

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, reason, CraftSessionTestPace.Actions);
        lifecycle.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        _world.Treasury.Should().Be(ShipyardFee);
        _world.CreditRequests.Should().Be(1);
        _world.Gold.Should().Be(1500);
    }

    [Test]
    public void SettleLeftover_AfterALogout_CreditsTheTreasuryOnce()
    {
        StartWithFee(Lifecycle());

        var lifecycle = Lifecycle(100, 100);
        lifecycle.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);
        lifecycle.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        _player.ActiveCraftSession.Should().BeNull();
        _world.Treasury.Should().Be(ShipyardFee);
        _world.CreditRequests.Should().Be(1);
    }

    [Test]
    public void ACrashBeforeTheStartsExport_RollsBackTheGoldAndTheFeeTogether()
    {
        StartWithFee(Lifecycle());
        var saved = Copy(_player);
        _world.CrashAndReload();

        Lifecycle(100, 100).RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);

        _world.Gold.Should().Be(2000, "the file still holds the credits");
        saved.ActiveCraftSession.Should().BeNull();
        saved.PendingStationFees.Should().BeNullOrEmpty();
        _world.Treasury.Should().Be(0, "no credit is duplicated");
        _world.CreditRequests.Should().Be(0);
    }

    [Test]
    public void ARestartAfterTheStartsExport_SettlesTheCraftAndCreditsTheFeeOnce()
    {
        StartWithFee(Lifecycle());
        _world.RunQueuedExport();
        var saved = Copy(_player);
        _world.CrashAndReload();

        var lifecycle = Lifecycle(100, 100);
        lifecycle.RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);
        lifecycle.RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);

        _world.Gold.Should().Be(1500, "the file holds the payment");
        saved.ActiveCraftSession.Should().BeNull();
        saved.PendingStationFees.Should().BeEmpty();
        _world.Treasury.Should().Be(ShipyardFee);
        _world.CreditRequests.Should().Be(1);
    }

    [Test]
    public void AStopBetweenTheTreasuryCreditAndThePlayersSave_IsNotCreditedAgainAtLogin()
    {
        var lifecycle = Lifecycle();
        var session = StartWithFee(lifecycle);
        _world.StopAfterTreasuryCredit = true;

        var stop = () => lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), CraftSessionTestPace.Actions);

        stop.Should().Throw<ServerStoppedException>();
        var saved = Copy(_player);
        saved.PendingStationFees.Should().ContainSingle("the player's save that forgets the fee never ran");
        _world.CrashAndReload();

        Lifecycle().RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);

        saved.PendingStationFees.Should().BeEmpty();
        _world.CreditRequests.Should().Be(2, "the login asked again");
        _world.Treasury.Should().Be(ShipyardFee, "the station's ledger already held the fee");
    }

    [Test]
    public void AQuickCraftFee_IsCreditedOnlyOnceTheFileHoldsIt_AndExactlyOnce()
    {
        var lifecycle = Lifecycle();
        _world.ExportNow();

        lifecycle.QueueStationFee(FakeCraftSessionWorld.PlayerHandle, _player, new StationFeeCharge(FakeCraftSessionWorld.StationId, ShipyardFee));
        lifecycle.CreditStationFees(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeFalse("the payment is not in a file yet");
        _world.Treasury.Should().Be(0);

        _world.RunQueuedExport();
        lifecycle.CreditStationFees(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeTrue();
        lifecycle.CreditStationFees(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeFalse();

        _world.Treasury.Should().Be(ShipyardFee);
        _world.CreditRequests.Should().Be(1);
    }

    [Test]
    public void AQuickCraftFeeTheFilePredates_IsDroppedAtLogin()
    {
        var lifecycle = Lifecycle();
        _world.ExportNow();
        lifecycle.QueueStationFee(FakeCraftSessionWorld.PlayerHandle, _player, new StationFeeCharge(FakeCraftSessionWorld.StationId, ShipyardFee));
        var saved = Copy(_player);
        _world.CrashAndReload();

        Lifecycle().RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);

        saved.PendingStationFees.Should().BeEmpty();
        _world.Treasury.Should().Be(0);
    }

    // ---- Component traits ------------------------------------------------------------------------------------------------

    [Test]
    public void Succeed_PassesTheCommittedComponentsOn_BeforeTheBlueprintTraitsAndTheDelivery()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle, FakeCraftSessionWorld.Blueprint);

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), CraftSessionTestPace.Actions);

        success.Should().NotBeNull();
        _world.ComponentTraitCalls.Should().ContainSingle();
        var call = _world.ComponentTraitCalls[0];
        call.Item.Should().Be(success.Item);
        call.Recipe.Should().Be(ShipRecipe, "a component variant's trait is looked up by its recipe");
        call.Components.Should().Equal(Committed);
        _world.StampOrder.Should().Equal("component-traits", "blueprint-bonuses");
        _world.Events.IndexOf("deliver").Should().BeGreaterThan(_world.Events.IndexOf($"create:{_recipe.Resref}"));
    }

    [Test]
    public void Succeed_WithoutABlueprint_StillPassesTheComponentTraitsOn()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), CraftSessionTestPace.Actions);

        _world.StampOrder.Should().Equal("component-traits");
    }

    [Test]
    public void Fail_ReturnsEveryKeptComponentAsItWasCommitted_SoItsTraitsSurvive()
    {
        var lifecycle = Lifecycle(100, 100);
        Start(lifecycle);

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        _world.Returned.Should().BeEquivalentTo(Committed, "a serialized component carries its trait local");
        _world.ComponentTraitCalls.Should().BeEmpty("a failed craft makes no item");
    }

    [Test]
    public void Fail_LosesOnlyTheRolledComponents_AsAnyOtherCraft()
    {
        var lifecycle = Lifecycle(1, 100);
        Start(lifecycle);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        settlement.ItemsLost.Should().Be(1);
        _world.Returned.Should().ContainSingle().Which.Should().BeOneOf(Committed);
    }

    [Test]
    public void LogoutMidCraft_SettlesAtTheNextWindowAndReturnsTheCommittedComponents()
    {
        Start(Lifecycle());

        Lifecycle(100, 100).SettleLeftover(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        _player.ActiveCraftSession.Should().BeNull();
        _world.Returned.Should().BeEquivalentTo(Committed);
    }

    [Test]
    public void ServerRestartMidCraft_TheSavedRecordKeepsTheComponentsAndReturnsThemAtLogin()
    {
        _world.ExportNow();
        Start(Lifecycle());
        _world.RunQueuedExport();

        var saved = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(_player))!;
        saved.ActiveCraftSession.Components.Should().Equal(Committed, "the record saves each component stack as committed");

        Lifecycle(100, 100).RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);

        saved.ActiveCraftSession.Should().BeNull();
        _world.Returned.Should().BeEquivalentTo(Committed);
    }

    // ---- Quick craft -----------------------------------------------------------------------------------------------------

    private sealed class RecordingQuickCraftWorld : IQuickCraftWorld
    {
        public List<RecipeType> ConsumedFor { get; } = new();
        public bool IsPlayerAvailable(uint player) => true;
        public void BeginBatch(uint player) { }
        public void PlayStationWork(uint player, SkillType skill) { }
        public void EndBatch(uint player) { }
        public void PlayItemAnimation(uint player, float seconds) { }
        public bool HasComponents(uint player, RecipeDetail recipe) => true;
        public bool CreateItem(uint player, RecipeDetail recipe) => true;
        public bool TryConsumeComponents(uint player, RecipeDetail recipe) => throw new AssertionException("the recipe type must be passed");

        public bool TryConsumeComponents(uint player, RecipeType recipeType, RecipeDetail recipe)
        {
            ConsumedFor.Add(recipeType);
            return true;
        }

        public void RemoveCreatedItem(uint player, RecipeDetail recipe) { }
        public void LogCraft(uint player, string text) { }
        public void Schedule(float seconds, Action action) => action();
        public void Message(uint player, string text) { }
    }

    [Test]
    public void QuickCraft_PassesTheRecipeToTheWorldForEveryItem_SoTheItemGetsItsComponentTraits()
    {
        var world = new RecordingQuickCraftWorld();
        var batch = new QuickCraftBatch(world, FakeCraftSessionWorld.PlayerHandle, ShipRecipe, _recipe, 3);

        batch.Start();

        batch.Completed.Should().Be(3);
        world.ConsumedFor.Should().Equal(ShipRecipe, ShipRecipe, ShipRecipe);
    }

    // ---- Shipyard build speed --------------------------------------------------------------------------------------------

    private static CircuitSetup Setup(int stationPercent) =>
        new(new CircuitParameters(6, 10, 50), 45, 89, 1296, 70, 45, 30, 30, 20, 0, null, stationPercent);

    [Test]
    public void ShipyardDroid_RaisesAssemblyOutputOnly_ByItsPercent()
    {
        var plain = Setup(0);
        var droid = Setup(30);

        droid.StationAssemblyPercent.Should().Be(30);
        droid.WireGain(0).Should().BeCloseTo((int)Math.Round(plain.WireGain(0) * 1.3, MidpointRounding.AwayFromZero), 1);
        droid.DischargeAssemblyGain(5).Should().BeGreaterThan(plain.DischargeAssemblyGain(5));
        droid.TuneGain(0).Should().Be(plain.TuneGain(0), "the droid speeds up the build, not its quality");
        droid.DischargeCalibrationGain(5).Should().Be(plain.DischargeCalibrationGain(5));
        droid.MaxAssembly.Should().Be(plain.MaxAssembly, "the piece needs the same Assembly; each action adds more");
    }

    [Test]
    public void ShipyardDroid_ANegativePercent_IsIgnored()
    {
        Setup(-10).WireGain(0).Should().Be(Setup(0).WireGain(0));
    }

    // ---- Wiring in the engine-side code ----------------------------------------------------------------------------------

    [Test]
    public void ComponentPlanning_CollectsStandIns_SpendsTheRareSlotFirst_AndOrdersTraitedComponents()
    {
        var source = ReadServer("Service", "CraftService", "Minigame", "NwnCraftSessionWorld.cs");
        var plan = Section(source, "private static List<(uint Stack, int Quantity)> PlanComponents", "public string ValidateStationFee");

        plan.Should().Contain("ShipComponents.IsStandInCandidate(recipe, item)");
        plan.IndexOf("ShipComponents.OrderForConsumption", StringComparison.Ordinal)
            .Should().BeLessThan(plan.IndexOf("ShipComponents.ReserveRareSlot", StringComparison.Ordinal));
        plan.IndexOf("ShipComponents.ReserveRareSlot", StringComparison.Ordinal)
            .Should().BeLessThan(plan.IndexOf("for (var index = stacks.Count - 1", StringComparison.Ordinal));
        plan.Should().Contain("GetItemStackSize(stacks[index]) - planned.GetValueOrDefault(stacks[index])",
            "a stand-in's unit is not spent twice");
        Section(source, "public bool TryTakeComponents", "public bool HasComponents").Should().Contain("PlanComponents(player, recipe)");
    }

    [Test]
    public void ShipBlueprints_StampBlueprintTraits_NotItemProperties()
    {
        var source = ReadServer("Service", "CraftService", "Minigame", "NwnCraftSessionWorld.cs");
        var apply = Section(source, "public void ApplyBlueprintBonuses", "private static void ApplyShipBlueprintTraits");
        apply.IndexOf("ShipBlueprintRules.IsResearchable(recipe)", StringComparison.Ordinal)
            .Should().BeLessThan(apply.IndexOf("BlueprintBonusPicker.PickBonus", StringComparison.Ordinal));

        var ship = Section(source, "private static void ApplyShipBlueprintTraits", "public bool AwardPendingXp");
        ship.Should().Contain("ShipBlueprintRules.PickItemBonuses(recipe, blueprint.ItemBonuses, blueprint.GuaranteedTraits, Random.Next)");
        ship.Should().Contain("traits.AddRange(blueprint.GuaranteedTraits)");
        ship.Should().Contain("ShipComponents.AddTraits(item, traits)");
        source.Should().Contain("ShipComponents.ApplyCraftedTraits(recipeType, recipe, item, components)");
    }

    [Test]
    public void QuickCraft_PaysTheStationFeePerItem_AndStampsTheTraitsOnTheItemItMade()
    {
        var source = ReadServer("Service", "CraftService", "Minigame", "NwnQuickCraftWorld.cs");
        var consume = Section(source, "public bool TryConsumeComponents(uint player, RecipeType recipeType, RecipeDetail recipe)", "public void RemoveCreatedItem");

        consume.IndexOf("TryTakeComponents(player, recipe, taken)", StringComparison.Ordinal)
            .Should().BeLessThan(consume.IndexOf("CraftSessionService.QueueStationFee(player, Craft.TakeStationFee(player, recipe))", StringComparison.Ordinal));
        Section(source, "public void EndBatch", "public void PlayItemAnimation").Should().Contain("FlushCharacter(player)")
            .And.Contain("CraftSessionService.CreditStationFees(player)");
        consume.Should().Contain("ApplyComponentTraits(item, recipeType, recipe, taken)");
        Section(source, "public bool HasComponents", "private readonly Dictionary").Should().Contain("Craft.GetStationFee(player, recipe)");
        ReadServer("Service", "CraftService", "Minigame", "QuickCraftService.cs")
            .Should().Contain("new QuickCraftBatch(NwnQuickCraftWorld.Instance, player, recipeType, Craft.GetRecipe(recipeType), quantity)");
    }

    [Test]
    public void CircuitWindow_AppliesTheShipyardDroid_AndNamesVariantRecipes()
    {
        var source = ReadServer("Feature", "GuiDefinition", "ViewModel", "CircuitCraftViewModel.cs");

        source.Should().Contain("Craft.GetStationProgressBonusPercent(Player, recipe)");
        source.Should().Contain("Craft.GetRecipeName(recipe)");
        ReadServer("Feature", "GuiDefinition", "ViewModel", "ForgeViewModel.cs").Should().Contain("Craft.GetRecipeName(_recipeDetail)");
        ReadServer("Feature", "GuiDefinition", "ViewModel", "KitchenViewModel.cs").Should().Contain("Craft.GetRecipeName(recipe)");

        var craft = ReadServer("Service", "Craft.cs");
        craft.Should().Contain("recipe.Requirements.OfType<RecipeShipyardRequirement>().Any()");
        craft.Should().Contain("StationBuildings.GetShipyardSpeedPercent(player)");
        craft.Should().Contain("StationBuildings.TakeShipyardFee(player, station)");
        ReadServer("Service", "StationBuildings.cs").Should().Contain("if (!dbStation.PaidCraftFees.Add(feeId))");
    }

    [Test]
    public void Research_PrototypeTechIsSpentWithTheStart_AndGrantedOncePerBlueprint()
    {
        var source = ReadServer("Feature", "GuiDefinition", "ViewModel", "ResearchViewModel.cs");
        var start = Section(source, "private void StartJob(ResearchJobDetails researchJob, bool spendPrototypeTech)", "public Action ClickCancelJob");

        start.IndexOf("Item.ReduceItemStack(prototypeTech, 1)", StringComparison.Ordinal)
            .Should().BeLessThan(start.IndexOf("ResearchExperimentService.StampStart(Player, dbResearchJob)", StringComparison.Ordinal),
                "the start's file stamp covers the Prototype Tech with the blueprint and the credits");
        start.Should().Contain("PrototypeTech = usesPrototypeTech");
        start.Should().Contain("ResearchExperimentLifecycle.Open(");
        source.Should().Contain("ShipBlueprintRules.PickGuaranteed(");
        source.Should().Contain("dbJob.PrototypeTech && ShipBlueprintRules.IsResearchable(recipe) && !blueprintDetails.PrototypeTechUsed");
    }

    private static string Section(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        from.Should().BeGreaterThanOrEqualTo(0, start);
        var to = source.IndexOf(end, from + start.Length, StringComparison.Ordinal);
        to.Should().BeGreaterThan(from, end);
        return source[from..to];
    }

    private static string ReadServer(params string[] path) =>
        File.ReadAllText(Path.Combine(new[] { RepositoryRoot, "SWLOR.Game.Server" }.Concat(path).ToArray()));

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                directory = directory.Parent;
            return directory!.FullName;
        }
    }
}
