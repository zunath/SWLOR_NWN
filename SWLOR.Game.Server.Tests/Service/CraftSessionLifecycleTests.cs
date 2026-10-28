using System.Linq;
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

    private int BaseXp => CraftMinigameXp.BaseXp(_recipe, 10);

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

    [TestCase(SkillType.Smithery)]
    [TestCase(SkillType.Engineering)]
    [TestCase(SkillType.Fabrication)]
    [TestCase(SkillType.Agriculture)]
    [TestCase(SkillType.Espionage)]
    public void Start_StartsTheStationWorkClipOfTheRecipesSkill(SkillType skill)
    {
        _recipe.Skill = skill;

        StartSession(Lifecycle());

        _world.StationAnimations.Should().Equal(skill);
    }

    [Test]
    public void Start_StartsNoStationWorkClip_WhenTheCraftIsRefused()
    {
        _world.Inventory["wire"] = 1;

        Lifecycle().Start(FakeCraftSessionWorld.PlayerHandle, _player, Request()).Succeeded.Should().BeFalse();

        _world.StationAnimations.Should().BeEmpty();
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
    public void Start_RefusesAndTakesNothing_WhenALaterEnhancementIsGone()
    {
        _world.Enhancements.Remove(FakeCraftSessionWorld.SecondEnhancement);

        var result = Lifecycle().Start(
            FakeCraftSessionWorld.PlayerHandle,
            _player,
            Request(NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement));

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Be("An enhancement is no longer in your inventory.");
        _player.ActiveCraftSession.Should().BeNull();
        _player.HeldCraftItems.Should().BeNullOrEmpty();
        _world.Returned.Should().BeEmpty();
        _world.Events.Should().NotContain(e => e.StartsWith("take") || e == "charge-blueprint" || e == "save");
        _world.Enhancements.Should().ContainKey(FakeCraftSessionWorld.FirstEnhancement);
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

        var success = lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

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
    public void Succeed_ANonStackingRecipeOfTwo_DeliversTwoItemsAndRecordsBoth()
    {
        _recipe.Quantity = 2;
        _world.NonStackingResrefs.Add(_recipe.Resref);
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _world.Crafted.Should().HaveCount(2);
        _world.Events.Should().ContainInOrder("create:test_item", "copy:test_item", "save");
        _player.PendingCraftReturns.SelectMany(entry => entry.Items).Should().BeEquivalentTo(_world.Crafted);
    }

    [Test]
    public void Succeed_UnitsThatDoNotFit_AreHeldSoTheFullQuantityIsKept()
    {
        _recipe.Quantity = 3;
        _world.NonStackingResrefs.Add(_recipe.Resref);
        _world.OutputRoom = 1;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _world.Crafted.Should().ContainSingle();
        _player.HeldCraftItems.Should().Equal("crafted test_item", "crafted test_item");
        _player.PendingCraftReturns.SelectMany(entry => entry.Items).Should().Equal(_world.Crafted);
        _world.Messages.Should().Contain(CraftSessionLifecycle.HeldItemsMessage);
    }

    [Test]
    public void Succeed_AnOutputThatNeverReachedTheInventory_IsHeld_NotLost()
    {
        _recipe.Quantity = 2;
        _world.OutputLost = true;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _world.Crafted.Should().BeEmpty();
        _player.HeldCraftItems.Should().ContainSingle();
        _player.PendingCraftReturns.Should().BeNullOrEmpty();
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void Succeed_ANonStackingOutputWithNoRoom_HoldsEveryUnit()
    {
        _recipe.Quantity = 2;
        _world.NonStackingResrefs.Add(_recipe.Resref);
        _world.OutputLost = true;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _player.HeldCraftItems.Should().HaveCount(2);
        _world.Crafted.Should().BeEmpty();
    }

    [Test]
    public void Succeed_AppliesTheBonusesBeforeTheOutputReachesTheInventory()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _world.Events.Should().ContainInOrder("create:test_item", "enhance", "finish");
    }

    [Test]
    public void Succeed_AStackingRecipeOfTwo_MakesNoCopies()
    {
        _recipe.Quantity = 2;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _world.Crafted.Should().ContainSingle();
        _world.Events.Should().NotContain(e => e.StartsWith("copy:"));
    }

    [Test]
    public void Succeed_SlowerThanTheModeledPace_PaysTheFullXp()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        // Three times the seconds the multipliers assume for 10 actions.
        _world.Advance(3 * 10 * CraftMinigameXp.ActionSeconds);
        lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), 10);

        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateXp(BaseXp, 0, true, 0.6f)));
    }

    [Test]
    public void Succeed_InHalfTheModeledTime_PaysHalfTheBaseXp()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        _world.Advance(10 * CraftMinigameXp.ActionSeconds / 2);
        var success = lifecycle.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), 10);

        var halfBase = CraftMinigameXp.ScaleBaseXp(BaseXp, 0.5);
        halfBase.Should().BeLessThan(BaseXp);
        success.Outcome.Xp.Should().Be(CraftOutcomeRules.CalculateXp(halfBase, 0, true, 0.6f));
        _world.XpGiven.Should().Equal((SkillType.Smithery, success.Outcome.Xp));
    }

    [Test]
    public void Succeed_WithNoActionsOrNoTime_PaysNoXpAndDoesNotDivideByZero()
    {
        var noTime = Lifecycle();
        var session = StartSession(noTime);

        noTime.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100), 10)
            .Outcome.Xp.Should().Be(0, "the clock did not move, so no time passed");

        _world.Inventory["ore"] = 5;
        _world.Inventory["wire"] = 2;
        var noActions = Lifecycle();
        var second = StartSession(noActions);
        _world.Advance(120);

        noActions.Succeed(FakeCraftSessionWorld.PlayerHandle, _player, second, new MinigameMeter(60, 100), 0)
            .Outcome.Xp.Should().Be(0, "no actions were taken");
    }

    [Test]
    public void Fail_AfterFewerSecondsThanTheModeledPace_PaysTheFailureXpInProportion()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        _world.Advance(6 * CraftMinigameXp.ActionSeconds / 2);
        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken, 6);

        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(CraftMinigameXp.ScaleBaseXp(BaseXp, 0.5), 2));
        settlement.Xp.Should().BeLessThan(CraftOutcomeRules.CalculateFailureXp(BaseXp, 2));
        _world.XpGiven.Should().Equal((SkillType.Smithery, settlement.Xp));
    }

    [Test]
    public void Fail_AtTheModeledPace_PaysTheFullFailureXp()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        _world.AdvanceByActions(6);
        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Broken, 6);

        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(BaseXp, 2));
    }

    [Test]
    public void Fail_WithNoWindowToCountTheActions_DoesNotScaleTheXp()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        settlement.Xp.Should().Be(CraftOutcomeRules.CalculateFailureXp(BaseXp, 2));
    }

    [Test]
    public void Succeed_AtTheSecondThreshold_AppliesBothEnhancements()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(85, 100));

        _world.AppliedEnhancements.Should().Equal("enh-a", "enh-b");
    }

    [Test]
    public void Succeed_OnARepeatCraft_SkipsTheFirstCraftBonus()
    {
        BaseXp.Should().BeGreaterThan(0);
        _player.CraftedRecipes[TestRecipe] = DateTime.UtcNow;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.XpGiven.Single().Xp.Should().Be(CraftOutcomeRules.CalculateXp(BaseXp, 0, false, 1f));
    }

    [Test]
    public void Succeed_CalledTwice_CreatesOneItem()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100)).Should().NotBeNull();
        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100)).Should().BeNull();

        _world.Events.Count(e => e == "create:test_item").Should().Be(1);
        _world.XpGiven.Should().ContainSingle();
    }

    [Test]
    public void Succeed_ForFoodWithABlueprint_AddsTheLinearDurationAndBlueprintBonuses()
    {
        _recipe.Category = RecipeCategoryType.Food;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.FoodBonuses.Should().Equal((300, 2));
        _world.BlueprintBonusCalls.Should().Be(1);
        _world.XpGiven.Single().Xp.Should().Be(CraftOutcomeRules.CalculateXp(BaseXp, 2, true, 1f));
    }

    [Test]
    public void Succeed_ForFoodBelowFullQuality_AppliesTheLinearDurationAndNoBlueprintCharges()
    {
        _recipe.Category = RecipeCategoryType.Food;
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(25, 100));

        _world.FoodBonuses.Should().Equal((75, 0));
    }

    [Test]
    public void EatingCraftedFood_AddsTheSecondsTheCraftStored()
    {
        // The consumable reads the local the session writes; the two must keep using one name.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();

        var source = File.ReadAllText(Path.Combine(
            directory!.FullName, "SWLOR.Game.Server", "Feature", "ItemDefinition", "ConsumableItemDefinition.cs"));

        source.Should().Contain("GetLocalInt(item, CraftOutcomeRules.FoodDurationBonusSecondsVariable)");
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
    public void Fail_WhileThePlayerIsDead_KeepsTheXpOnTheRecordInsteadOfLosingIt()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle, FakeCraftSessionWorld.Blueprint);
        _world.PlayerAlive = false;

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Died);

        _world.XpGiven.Should().BeEmpty("a dead player cannot be given XP");
        _player.PendingCraftXp.Should().Equal(new Dictionary<SkillType, int> { [SkillType.Smithery] = settlement.Xp });
        settlement.Xp.Should().BePositive();
    }

    [Test]
    public void Succeed_SavesTheXpEntitlementWithTheSettlement_ThenGrantsItFromThere()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        _world.SavedRecords.Clear();

        var success = lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        success.Outcome.Xp.Should().BePositive();
        var entitled = new Dictionary<SkillType, int> { [SkillType.Smithery] = success.Outcome.Xp };
        _world.SavedRecords.Should().HaveCount(2);
        _world.SavedRecords[0].SessionActive.Should().BeFalse("the settlement save ends the craft");
        _world.SavedRecords[0].PendingXp.Should().Equal(entitled, "the same save that ends the craft carries the XP it earned");
        _world.SavedRecords[1].PendingXp.Should().BeEmpty("the save that gives the XP also clears the entitlement");
        _world.SavedPendingAtXp.Should().ContainSingle().Which.Should().Equal(entitled, "the entitlement stays saved until the award's own save");
        _world.XpGiven.Should().Equal((SkillType.Smithery, success.Outcome.Xp));
        _player.PendingCraftXp.Should().BeEmpty();
    }

    [Test]
    public void Fail_ForALivingPlayer_SavesTheXpEntitlementWithTheSettlement_ThenGrantsItFromThere()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.SavedRecords.Clear();

        var settlement = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        settlement.Xp.Should().BePositive();
        _world.SavedRecords[0].SessionActive.Should().BeFalse();
        _world.SavedRecords[0].PendingXp.Should().Equal(new Dictionary<SkillType, int> { [SkillType.Smithery] = settlement.Xp });
        _world.SavedRecords.Last().PendingXp.Should().BeEmpty();
        _world.SavedPendingAtXp.Should().ContainSingle().Which.Should().ContainKey(SkillType.Smithery, "the entitlement stays saved until the award's own save");
        _world.XpGiven.Should().Equal((SkillType.Smithery, settlement.Xp));
    }

    [Test]
    public void Succeed_WhenTheServerStopsAfterTheSettlementSaveAndBeforeTheAward_GivesTheXpExactlyOnceLater()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        _world.StopBeforeXpGrant = true;

        var stop = () => lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        stop.Should().Throw<ServerStoppedException>();
        _world.XpGiven.Should().BeEmpty("the award never ran");
        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(_player));
        restored.ActiveCraftSession.Should().BeNull("the item is recoverable");
        restored.PendingCraftXp.Should().ContainKey(SkillType.Smithery, "the XP survived the stop");
        var owed = restored.PendingCraftXp[SkillType.Smithery];
        owed.Should().BePositive();

        _world.StopBeforeXpGrant = false;
        var afterRestart = Lifecycle();
        afterRestart.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, restored).Should().BeTrue();
        afterRestart.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, restored).Should().BeFalse("it is given exactly once");
        afterRestart.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, restored, CraftSessionEndReason.Interrupted);

        _world.XpGiven.Should().Equal((SkillType.Smithery, owed));
        restored.PendingCraftXp.Should().BeEmpty();
    }

    [Test]
    public void Fail_WhenTheServerStopsAfterTheSettlementSaveAndBeforeTheAward_GivesTheXpExactlyOnceLater()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.StopBeforeXpGrant = true;

        var stop = () => lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        stop.Should().Throw<ServerStoppedException>();
        _world.XpGiven.Should().BeEmpty();
        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(_player));
        restored.ActiveCraftSession.Should().BeNull();
        restored.PendingCraftXp.Should().ContainKey(SkillType.Smithery);
        var owed = restored.PendingCraftXp[SkillType.Smithery];
        owed.Should().BePositive();

        _world.StopBeforeXpGrant = false;
        var afterRestart = Lifecycle();
        afterRestart.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, restored, CraftSessionEndReason.Interrupted);
        afterRestart.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, restored).Should().BeFalse();

        _world.XpGiven.Should().Equal((SkillType.Smithery, owed));
    }

    [Test]
    public void GrantPendingXp_GivesTheWaitingXpOnceThePlayerIsAlive_AndNeverTwice()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.PlayerAlive = false;
        var xp = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Died).Xp;

        lifecycle.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeFalse("the player is still dead");
        _world.XpGiven.Should().BeEmpty();
        _player.PendingCraftXp.Should().NotBeEmpty();

        _world.PlayerAlive = true;
        lifecycle.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeTrue();
        lifecycle.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeFalse("a respawn and a relog must not both give it");
        lifecycle.SettleLeftover(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        _world.XpGiven.Should().Equal((SkillType.Smithery, xp));
        _player.PendingCraftXp.Should().BeEmpty();
    }

    [Test]
    public void GrantPendingXp_SumsTheXpOfSeveralDeathsPerSkill()
    {
        var lifecycle = Lifecycle(100, 100, 100, 100);
        _world.PlayerAlive = false;

        StartSession(lifecycle);
        var first = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Died).Xp;
        _world.Inventory["ore"] = 5;
        _world.Inventory["wire"] = 2;
        StartSession(lifecycle);
        var second = lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Died).Xp;

        _world.PlayerAlive = true;
        lifecycle.GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, _player);

        _world.XpGiven.Should().Equal((SkillType.Smithery, first + second));
    }

    [Test]
    public void GrantPendingXp_WithNothingWaiting_DoesNothing()
    {
        _player.PendingCraftXp = null;

        Lifecycle().GrantPendingXp(FakeCraftSessionWorld.PlayerHandle, _player).Should().BeFalse();

        _world.XpGiven.Should().BeEmpty();
        _world.SaveCount.Should().Be(0);
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
        _world.Messages.Should().Contain(CraftSessionLifecycle.HeldItemsMessage);

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
        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100))
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
    public void Start_WritesTheCharacterFile_AfterTheRecordIsSaved_SoTheFileLosesTheMaterialsTheRecordHolds()
    {
        StartSession(Lifecycle(), NoItem, FakeCraftSessionWorld.FirstEnhancement);

        _world.PersistedAtSave.Should().Equal(new[] { _world.SaveCount }, "one export, after the record that holds the materials was saved");
    }

    [Test]
    public void Start_WhenALaterEnhancementIsGone_WritesNoCharacterFile_BecauseNothingMoved()
    {
        _world.Enhancements.Remove(FakeCraftSessionWorld.SecondEnhancement);

        Lifecycle().Start(
            FakeCraftSessionWorld.PlayerHandle,
            _player,
            Request(NoItem, FakeCraftSessionWorld.FirstEnhancement, FakeCraftSessionWorld.SecondEnhancement)).Succeeded.Should().BeFalse();

        _world.PersistedAtSave.Should().BeEmpty();
        _world.SaveCount.Should().Be(0);
    }

    [Test]
    public void Start_WhenRefused_WritesNoCharacterFile_BecauseNothingMoved()
    {
        _world.Inventory["wire"] = 1;

        Lifecycle().Start(FakeCraftSessionWorld.PlayerHandle, _player, Request()).Succeeded.Should().BeFalse();

        _world.PersistedAtSave.Should().BeEmpty();
    }

    [Test]
    public void Succeed_WritesTheCharacterFile_AfterTheRecordClearsTheSession()
    {
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        _world.PersistedAtSave.Clear();
        var savesBefore = _world.SaveCount;

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(60, 100));

        _world.PersistedAtSave.Should().HaveCount(1);
        _world.PersistedAtSave.Single().Should().BeGreaterThan(savesBefore, "the export follows the record save that cleared the session");
        _player.ActiveCraftSession.Should().BeNull();
    }

    [Test]
    public void Fail_WritesTheCharacterFile_WhenComponentsCameBack_AndNotWhenNothingDid()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.PersistedAtSave.Clear();

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        _world.Returned.Should().NotBeEmpty();
        _world.PersistedAtSave.Should().NotBeEmpty("the returned items are in the inventory, so the file must have them");
        _world.PersistedAtSave.Last().Should().Be(_world.SaveCount - 1, "after the record that cleared the session and the held items, before only the save that clears the XP entitlement");

        var lost = Lifecycle(0, 0);
        _world.Inventory["ore"] = 5;
        _world.Inventory["wire"] = 2;
        StartSession(lost);
        _world.PersistedAtSave.Clear();

        lost.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        _world.PersistedAtSave.Should().BeEmpty("every item was lost, so the file already lacks them");
    }

    [Test]
    public void SettleLeftover_AtLogin_WritesTheCharacterFileAfterTheRefund()
    {
        StartSession(Lifecycle());
        var restored = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(_player));
        _world.PersistedAtSave.Clear();

        Lifecycle(100, 100).SettleLeftover(FakeCraftSessionWorld.PlayerHandle, restored, CraftSessionEndReason.Interrupted);

        _world.PersistedAtSave.Should().NotBeEmpty();
        _world.PersistedAtSave.Last().Should().Be(_world.SaveCount - 1, "only the save that clears the XP entitlement follows the export");
    }

    [Test]
    public void ReturnHeldItems_WritesTheCharacterFile_OnlyWhenSomethingWasGivenBack()
    {
        _player.HeldCraftItems.Add("ore x3");
        _world.Room = 0;
        var lifecycle = Lifecycle();

        lifecycle.ReturnHeldItems(FakeCraftSessionWorld.PlayerHandle, _player);
        _world.PersistedAtSave.Should().BeEmpty();

        _world.Room = int.MaxValue;
        lifecycle.ReturnHeldItems(FakeCraftSessionWorld.PlayerHandle, _player);
        _world.PersistedAtSave.Should().Equal(new[] { _world.SaveCount });
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

    // ---- Crash safety: the record saves at once, the character file only on an export that follows. ----

    private static Player Copy(Player player) =>
        JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;

    /// <summary>A crash before any queued export ran, and the login after it: the record as saved, the inventory as the file had it.</summary>
    private Player CrashAndLogIn(Player live = null)
    {
        var saved = Copy(live ?? _player);
        _world.CrashAndReload();
        return saved;
    }

    private CraftSettlement LogIn(Player saved, params int[] rolls) =>
        Lifecycle(rolls).RecoverAtLogin(FakeCraftSessionWorld.PlayerHandle, saved, CraftSessionEndReason.Interrupted);

    [Test]
    public void RecoverAtLogin_AfterACrashBetweenTheDebitAndTheExport_ReturnsNothing()
    {
        _world.ExportNow();
        StartSession(Lifecycle(), FakeCraftSessionWorld.Blueprint, FakeCraftSessionWorld.FirstEnhancement);
        _world.Inventory["ore"].Should().Be(2, "the components are out of the inventory in memory");

        var saved = CrashAndLogIn();
        var settlement = LogIn(saved, 100, 100, 100);

        settlement.Settled.Should().BeFalse();
        saved.ActiveCraftSession.Should().BeNull("the file still has everything the session took");
        _world.Returned.Should().BeEmpty("a refund on top of the file would duplicate the components");
        _world.Inventory["ore"].Should().Be(5);
        _world.Inventory["wire"].Should().Be(2);
        _world.XpGiven.Should().BeEmpty();
    }

    [Test]
    public void RecoverAtLogin_ComparesAgainstTheLoadedFile_EvenWhenAnotherLoginStepAdvancedTheSequenceFirst()
    {
        _world.ExportNow();
        StartSession(Lifecycle(), FakeCraftSessionWorld.Blueprint, FakeCraftSessionWorld.FirstEnhancement);

        var saved = CrashAndLogIn();
        // Another login handler (the Search finds return) stamps a transfer before the craft recovery runs.
        _world.AdvanceFileSequence(FakeCraftSessionWorld.PlayerHandle);
        _world.AdvanceFileSequence(FakeCraftSessionWorld.PlayerHandle);
        LogIn(saved, 100, 100, 100);

        saved.ActiveCraftSession.Should().BeNull();
        _world.Returned.Should().BeEmpty("the loaded file still has the components, whatever ran before the recovery");
        _world.Inventory["ore"].Should().Be(5);
    }

    [Test]
    public void RecoverAtLogin_AfterTheDebitWasExported_RefundsOnce()
    {
        _world.ExportNow();
        StartSession(Lifecycle());
        _world.RunQueuedExport();

        var saved = CrashAndLogIn();
        var settlement = LogIn(saved, 100, 100);

        settlement.Settled.Should().BeTrue();
        _world.Returned.Should().Equal("ore x3", "wire x2");
        saved.ActiveCraftSession.Should().BeNull();

        // The refund is exported and the player logs in again: nothing more comes back.
        _world.RunQueuedExport();
        var again = CrashAndLogIn(saved);
        LogIn(again, 100, 100).Settled.Should().BeFalse();
        _world.Returned.Should().Equal("ore x3", "wire x2");
    }

    [Test]
    public void RecoverAtLogin_AfterACrashDuringTheRefund_GivesTheRefundBackExactlyOnce()
    {
        _world.ExportNow();
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.RunQueuedExport();
        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);
        _world.Returned.Should().Equal("ore x3", "wire x2");
        _player.PendingCraftReturns.Should().ContainSingle("the record must say the refund is not in a file yet");

        var saved = CrashAndLogIn();
        _world.Returned.Should().BeEmpty("the file predates the refund");
        LogIn(saved);

        _world.Returned.Should().Equal("ore x3", "wire x2");
        saved.PendingCraftReturns.Should().ContainSingle("the redo waits for the file that will hold it");

        // A second crash before the redo is exported redoes it again, once; an export ends it.
        var second = CrashAndLogIn(saved);
        LogIn(second);
        _world.Returned.Should().Equal("ore x3", "wire x2");

        _world.RunQueuedExport();
        var third = CrashAndLogIn(second);
        LogIn(third);
        _world.Returned.Should().Equal("ore x3", "wire x2");
        third.PendingCraftReturns.Should().BeEmpty();
    }

    [Test]
    public void RecoverAtLogin_AfterACrashDuringSucceed_CreatesTheFinishedItemExactlyOnce()
    {
        _world.ExportNow();
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        _world.RunQueuedExport();
        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100)).Should().NotBeNull();
        _world.Crafted.Should().Equal("crafted test_item");

        var saved = CrashAndLogIn();
        _world.Crafted.Should().BeEmpty("the file predates the finished item");
        saved.ActiveCraftSession.Should().BeNull("the record says the craft is over");
        LogIn(saved);

        _world.Returned.Should().Equal("crafted test_item");

        _world.RunQueuedExport();
        var again = CrashAndLogIn(saved);
        LogIn(again);
        _world.Returned.Should().Equal("crafted test_item");
    }

    [Test]
    public void RecoverAtLogin_AfterAnExportedSucceed_DoesNotCreateTheItemAgain()
    {
        _world.ExportNow();
        var lifecycle = Lifecycle();
        var session = StartSession(lifecycle);
        _world.RunQueuedExport();
        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));
        _world.RunQueuedExport();

        var saved = CrashAndLogIn();
        LogIn(saved);

        _world.Crafted.Should().Equal("crafted test_item");
        _world.Returned.Should().BeEmpty();
        saved.PendingCraftReturns.Should().BeEmpty();
    }

    [Test]
    public void Fail_BeforeTheDebitWasExported_WritesTheFileFirst_SoACrashCannotDuplicateTheRefund()
    {
        _world.ExportNow();
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);
        _world.Flushes.Should().Be(1);

        var saved = CrashAndLogIn();
        _world.Inventory["ore"].Should().Be(2, "the flushed file lacks the components");
        LogIn(saved);

        _world.Returned.Should().Equal("ore x3", "wire x2");
    }

    [Test]
    public void Fail_AfterTheDebitWasExported_DoesNotWriteTheFileAgain()
    {
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.RunQueuedExport();

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);

        _world.Flushes.Should().Be(0);
    }

    [Test]
    public void ReturnHeldItems_AfterACrashBeforeTheExport_GivesTheHeldItemsBackExactlyOnce()
    {
        _world.ExportNow();
        _player.HeldCraftItems.Add("ore x3");
        var lifecycle = Lifecycle();

        lifecycle.ReturnHeldItems(FakeCraftSessionWorld.PlayerHandle, _player);
        _player.HeldCraftItems.Should().BeEmpty();

        var saved = CrashAndLogIn();
        _world.Returned.Should().BeEmpty();
        LogIn(saved);

        _world.Returned.Should().Equal("ore x3");
        saved.HeldCraftItems.Should().BeEmpty();
    }

    [Test]
    public void RecoverAtLogin_WithASessionSavedBeforeSequencesExisted_SettlesAsItAlwaysDid()
    {
        _player.ActiveCraftSession = new CraftSessionRecord
        {
            SessionId = "legacy",
            Recipe = TestRecipe,
            Skill = SkillType.Smithery,
            Components = new List<string> { "ore x3", "wire x2" },
            Enhancements = new List<string>()
        };
        _player.PendingCraftReturns = null;

        var saved = Copy(_player);
        saved.ActiveCraftSession.DebitSequence.Should().Be(0);
        var settlement = LogIn(saved, 100, 100);

        settlement.Settled.Should().BeTrue();
        _world.Returned.Should().Equal("ore x3", "wire x2");
    }

    [Test]
    public void Start_DropsPendingReturnsTheFileNowHolds()
    {
        _world.ExportNow();
        var lifecycle = Lifecycle(100, 100);
        StartSession(lifecycle);
        _world.RunQueuedExport();
        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.WindowClosed);
        _player.PendingCraftReturns.Should().ContainSingle();
        _world.RunQueuedExport();
        _world.Inventory["ore"] = 5;
        _world.Inventory["wire"] = 2;

        StartSession(lifecycle);

        _player.PendingCraftReturns.Should().BeEmpty();
    }
}
