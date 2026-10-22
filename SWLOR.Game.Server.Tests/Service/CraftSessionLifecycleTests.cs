using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>Covers the craft session lifecycle: what is committed and saved, and how every way a craft ends settles it.</summary>
public class CraftSessionLifecycleTests
{
    private const RecipeType TestRecipe = RecipeType.VenomCoating1;
    private const uint NoItem = OBJECT_INVALID;

    private FakeCraftSessionWorld _world;
    private Player _player;
    private RecipeDetail _recipe;

    [SetUp]
    public void SetUp()
    {
        _recipe = new RecipeDetail
        {
            Resref = "test_item",
            Skill = SkillType.Smithery,
            Level = 10,
            Components = new Dictionary<string, int> { ["ore"] = 3, ["wire"] = 2 },
            EnhancementType = RecipeEnhancementType.Armor,
            EnhancementSlots = 2
        };

        _world = new FakeCraftSessionWorld();
        _world.Recipes[TestRecipe] = _recipe;
        _world.Inventory["ore"] = 5;
        _world.Inventory["wire"] = 2;
        _world.Enhancements[FakeCraftSessionWorld.FirstEnhancement] = "enh-a";
        _world.Enhancements[FakeCraftSessionWorld.SecondEnhancement] = "enh-b";

        _player = new Player("player-1");
        _player.Skills[SkillType.Smithery] = new PlayerSkill { Rank = 10 };
    }

    private CraftSessionLifecycle Lifecycle(params int[] rolls) => new(_world, new ScriptedMinigameRandom(rolls));

    private CraftSessionStartRequest Request(uint blueprint = NoItem, params uint[] enhancements) =>
        new(TestRecipe, blueprint, enhancements);

    private CraftSession StartSession(CraftSessionLifecycle lifecycle, uint blueprint = NoItem, params uint[] enhancements)
    {
        var result = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, _player, Request(blueprint, enhancements));
        result.Succeeded.Should().BeTrue(result.Message);
        return result.Session;
    }

    private int BaseXp => Craft.GetBaseRecipeXP(_recipe, 10);

    [Test]
    public void Start_CommitsTheMaterialsAndSavesThemOnThePlayerBeforeTheStationLocksIn()
    {
        var lifecycle = Lifecycle();

        var session = StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement);

        var record = _player.ActiveCraftSession;
        record.Should().NotBeNull();
        record.SessionId.Should().Be(session.SessionId);
        record.Recipe.Should().Be(TestRecipe);
        record.Skill.Should().Be(SkillType.Smithery);
        record.Components.Should().Equal("ore x3", "wire x2");
        record.Enhancements.Should().Equal("enh-a", "enh-b");
        record.HasBlueprint.Should().BeFalse();
        _world.Inventory["ore"].Should().Be(2);
        _world.Inventory["wire"].Should().Be(0);
        _world.Events.IndexOf("save").Should().BeLessThan(_world.Events.IndexOf("begin-work"));
    }

    [Test]
    public void Start_KeepsEmptyEnhancementSlotsInPlaceSoSlotNumbersSurvive()
    {
        var lifecycle = Lifecycle();

        StartSession(lifecycle, NoItem, NoItem, FakeCraftSessionWorld.SecondEnhancement);

        _player.ActiveCraftSession.Enhancements.Should().Equal(string.Empty, "enh-b");
    }

    [Test]
    public void Start_WithABlueprint_ChargesItOnceAndRecordsItsLevel()
    {
        var lifecycle = Lifecycle();

        var session = StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        _world.Events.Count(e => e == "charge-blueprint").Should().Be(1);
        _player.ActiveCraftSession.HasBlueprint.Should().BeTrue();
        _player.ActiveCraftSession.BlueprintLevel.Should().Be(2);
        session.Blueprint.Should().BeSameAs(_world.BlueprintAfterCharge);
    }

    [Test]
    public void Start_RefusesAndTakesNothing_WhenAComponentIsMissing()
    {
        _world.Inventory["wire"] = 1;

        var result = Lifecycle().Start(FakeCraftSessionWorld.PlayerHandle, _player, Request());

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("Missing components!");
        _player.ActiveCraftSession.Should().BeNull();
        _world.Inventory["ore"].Should().Be(5);
        _world.Events.Should().NotContain("begin-work");
    }

    [Test]
    public void Start_RefusesWhenTheRecipeRequirementsAreNotMet()
    {
        _world.MeetsRequirements = false;

        var result = Lifecycle().Start(FakeCraftSessionWorld.PlayerHandle, _player, Request());

        result.Message.Should().Be("Recipe requirements not met!");
        _world.Inventory["ore"].Should().Be(5);
    }

    [Test]
    public void Start_RefusesAnInvalidBlueprint_BeforeTakingAnything()
    {
        _world.BlueprintProblem = "No licensed runs remaining!";

        var result = Lifecycle().Start(FakeCraftSessionWorld.PlayerHandle, _player, Request(FakeCraftSessionWorld.Blueprint));

        result.Message.Should().Be("No licensed runs remaining!");
        _world.Inventory["ore"].Should().Be(5);
        _world.Events.Should().NotContain("charge-blueprint");
    }

    [Test]
    public void Start_ReturnsTheComponentsAndEnhancements_WhenALaterEnhancementIsGone()
    {
        _world.Enhancements.Remove(FakeCraftSessionWorld.SecondEnhancement);

        var result = Lifecycle().Start(
            FakeCraftSessionWorld.PlayerHandle,
            _player,
            Request(NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("An enhancement is no longer in your inventory.");
        _player.ActiveCraftSession.Should().BeNull();
        _world.Returned.Should().BeEquivalentTo("ore x3", "wire x2", "enh-a");
        _world.Events.Should().NotContain("charge-blueprint");
    }

    [Test]
    public void Start_SettlesAnOlderSavedCraftFirst()
    {
        var lifecycle = Lifecycle(100, 100, 100, 100);
        StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement);
        _world.Inventory["ore"] = 3;
        _world.Inventory["wire"] = 2;
        var firstId = _player.ActiveCraftSession.SessionId;

        var second = StartSession(lifecycle);

        second.SessionId.Should().NotBe(firstId);
        _world.XpGiven.Should().ContainSingle("the replaced craft settled as a failure");
        _world.Returned.Should().HaveCount(3, "every roll kept an item");
    }

    [Test]
    public void Succeed_CreatesTheItemAppliesTheOutcomeAndClearsTheSavedCraft()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement);
        _world.Events.Clear();

        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        success.Should().NotBeNull();
        success.Outcome.QualityPercent.Should().Be(60);
        _world.Events.Should().ContainInOrder("create:test_item", "save", "end-work", "xp");
        _world.VendorValues.Should().Equal(CraftOutcomeRules.CalculateVendorValueBonus(10, 0.6f));
        _world.AppliedEnhancements.Should().Equal("enh-a");
        _world.XpGiven.Should().ContainSingle()
            .Which.Should().Be((SkillType.Smithery, CraftOutcomeRules.CalculateXp(BaseXp, 0, true, 0.6f)));
        _player.ActiveCraftSession.Should().BeNull();
        _player.CraftedRecipes.Should().ContainKey(TestRecipe);
        _world.Messages.Should().HaveCount(2);
    }

    [Test]
    public void Succeed_AtTheSecondThreshold_AppliesBothEnhancements()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(85, 100));

        _world.AppliedEnhancements.Should().Equal("enh-a", "enh-b");
    }

    [Test]
    public void Succeed_OnARepeatCraft_SkipsTheFirstCraftBonus()
    {
        BaseXp.Should().BeGreaterThan(0);
        _player.CraftedRecipes[TestRecipe] = DateTime.UtcNow;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.XpGiven.Single().Xp.Should().Be(CraftOutcomeRules.CalculateXp(BaseXp, 0, false, 1f));
    }

    [Test]
    public void Succeed_CalledTwice_CreatesOneItem()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100)).Should().NotBeNull();
        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100)).Should().BeNull();

        _world.Events.Count(e => e == "create:test_item").Should().Be(1);
        _world.XpGiven.Should().ContainSingle();
    }

    [Test]
    public void Succeed_ForFoodWithABlueprint_AddsTheLinearDurationAndBlueprintBonuses()
    {
        _recipe.Category = RecipeCategoryType.Food;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.FoodBonuses.Should().Equal((1, 2));
        _world.BlueprintBonusCalls.Should().Be(1);
        _world.XpGiven.Single().Xp.Should().Be(CraftOutcomeRules.CalculateXp(BaseXp, 2, true, 1f));
    }

    [Test]
    public void Fail_EachItemComesBackOnARollAboveSixtyFive_EnhancementsRollingFirst()
    {
        // Rolls: enhancement 1 kept (66), enhancement 2 lost (65), ore kept (100), wire lost (1).
        var lifecycle = Lifecycle(66, 65, 100, 1);
        StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        settlement.Settled.Should().BeTrue();
        settlement.ItemsLost.Should().Be(2);
        settlement.ItemsKept.Should().Be(2);
        _world.Returned.Should().Equal("enh-a", "ore x3");
        _player.ActiveCraftSession.Should().BeNull();
        _player.HeldCraftItems.Should().BeEmpty();
        _world.Events.Should().ContainInOrder("save", "end-work", "xp");
    }

    [Test]
    public void Fail_StillGivesFifteenPercentOfTheBaseXp()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken);

        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(BaseXp, 2));
        _world.XpGiven.Should().Equal((SkillType.Smithery, settlement.Xp));
    }

    [Test]
    public void Fail_HoldsItemsThatDoNotFitUntilThereIsRoom()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.Room = 0;

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        _player.HeldCraftItems.Should().Equal("ore x3", "wire x2");
        _world.Returned.Should().BeEmpty();
        _world.Messages.Should().Contain(Item.ItemsHeldMessage);

        _world.Room = 1;
        lifecycle.ReturnHeldItems(FakeCraftSessionWorld.PlayerHandle, _player).Should().Be(1);
        _player.HeldCraftItems.Should().Equal("wire x2");

        _world.Room = int.MaxValue;
        lifecycle.ReturnHeldItems(FakeCraftSessionWorld.PlayerHandle, _player).Should().Be(0);
        _player.HeldCraftItems.Should().BeEmpty();
        _world.Returned.Should().Equal("ore x3", "wire x2");
    }

    [Test]
    public void Fail_WithNothingSaved_DoesNothing()
    {
        var settlement = Lifecycle().Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Died);

        settlement.Settled.Should().BeFalse();
        _world.XpGiven.Should().BeEmpty();
        _world.Events.Should().BeEmpty();
    }

    [TestCase(CraftSessionEndReason.WindowClosed, true)]
    [TestCase(CraftSessionEndReason.Died, true)]
    [TestCase(CraftSessionEndReason.Interrupted, true)]
    [TestCase(CraftSessionEndReason.Broken, false)]
    public void Fail_TellsThePlayerWhenNoWindowIsLeftToShowIt(CraftSessionEndReason reason, bool messages)
    {
        var lifecycle = Lifecycle(1, 1);
        StartSession(lifecycle);

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, reason);

        (_world.Messages.Count > 0).Should().Be(messages);
    }

    [Test]
    public void Fail_AfterTheWindowAlreadySettled_DoesNotSettleTwice()
    {
        var lifecycle = Lifecycle(100, 100);
        var session = StartSession(lifecycle);

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Died);
        var again = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        again.Settled.Should().BeFalse();
        _world.XpGiven.Should().ContainSingle();
        CraftSessionLifecycle.IsActive(_player, session).Should().BeFalse();
        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100))
            .Should().BeNull("a craft settled by death cannot also succeed");
    }

    [Test]
    public void ASavedCraft_SurvivesALogoutAndSettlesAsAFailureAtTheNextLogin()
    {
        var firstRun = Lifecycle();
        var session = StartSession(firstRun, FakeCraftSessionWorld.Blueprint, FakeCraftSessionWorld.FirstEnhancement);
        CraftSessionLifecycle.IsActive(_player, session).Should().BeTrue();

        // The server stops and starts: the player record is all that survives.
        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(_player));
        _world.Returned.Clear();
        _world.XpGiven.Clear();
        var afterRestart = Lifecycle(100, 100, 100);

        var settlement = afterRestart.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, restored, CraftSessionEndReason.Interrupted);

        settlement.Settled.Should().BeTrue();
        settlement.ItemsKept.Should().Be(3);
        _world.Returned.Should().Equal("enh-a", "ore x3", "wire x2");
        restored.ActiveCraftSession.Should().BeNull();
        _world.XpGiven.Single().Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(BaseXp, 2));
    }

    [Test]
    public void SettleLeftover_WithNoCraft_StillReturnsHeldItems()
    {
        _player.HeldCraftItems.Add("ore x3");

        var settlement = Lifecycle().SettleLeftover(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        settlement.Settled.Should().BeFalse();
        _world.Returned.Should().Equal("ore x3");
        _player.HeldCraftItems.Should().BeEmpty();
    }

    [Test]
    public void APlayerRecordFromBeforeCraftSessionsLoadsWithEmptyDefaults()
    {
        const string legacy = "{\"Id\":\"old\",\"Version\":23,\"Name\":\"Old Timer\",\"CraftedRecipes\":{}}";

        var player = JsonConvert.DeserializeObject<Player>(legacy)!;

        player.ActiveCraftSession.Should().BeNull();
        player.HeldCraftItems.Should().NotBeNull().And.BeEmpty();
        player.CraftMinigameHelpSeen.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    public void ACraftSessionRecord_RoundTripsThroughThePlayerRecord()
    {
        var lifecycle = Lifecycle();
        StartSession(lifecycle, FakeCraftSessionWorld.Blueprint, NoItem, FakeCraftSessionWorld.SecondEnhancement);
        _player.CraftMinigameHelpSeen[CraftMinigameType.Forge] = new DateTime(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);
        _player.HeldCraftItems.Add("held");

        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(_player))!;

        restored.ActiveCraftSession.Should().BeEquivalentTo(_player.ActiveCraftSession);
        restored.ActiveCraftSession.Enhancements.Should().Equal(string.Empty, "enh-b");
        restored.HeldCraftItems.Should().Equal("held");
        restored.CraftMinigameHelpSeen.Should().ContainKey(CraftMinigameType.Forge);
    }
}
