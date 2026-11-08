using FluentAssertions;
using NUnit.Framework;
using static SWLOR.NWN.API.NWScript.NWScript;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SlicingService;
using SWLOR.Game.Server.Tests.Support;
using Random = SWLOR.Game.Server.Service.Random;
using Slicing = SWLOR.Game.Server.Service.SlicingService.Slicing;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// The slicing attempts: the claim on a target, the play of a board, and how an attempt ends. The board rules are
/// covered by <c>SlicingTests</c>; the rolls are pinned by seeding the shared generator and replaying the draws.
/// </summary>
[TestFixture]
public class SlicingSessionServiceTests
{
    private const uint Player = 100;
    private const uint Target = 200;
    private const uint ToolItem = 300;
    private const uint ObjectInvalid = 0x7F000000;
    private const int Now = 1_000_000;

    private sealed class TestSlicing : SlicingSessionService
    {
        public TestSlicing(IDBService db, ISlicingService slicing, IItemService item, IGuiService gui, ISpawnService spawn, ICacheService cache)
            : base(db, slicing, item, gui, spawn, cache, AmbientServices.Of<IPerkService>(), AmbientServices.Of<ISkillService>(), AmbientServices.Of<IStatService>())
        {
        }

        public int PerkLevel { get; set; } = 5;
        public int Lockpicking { get; set; }
        public int Seconds { get; set; } = Now;
        public List<string> Logs { get; } = new();
        public List<(uint Player, int Xp)> XpGrants { get; } = new();

        protected override int GetPerkLevel(uint player, PerkType perk) => PerkLevel;
        protected override int GetLockpicking(uint player) => Lockpicking;
        protected override int CalculateEspionageXp(PerkType perk, int tier, int rank) => 100 * tier + rank;
        protected override void GiveEspionageXp(uint player, int xp) => XpGrants.Add((player, xp));
        protected override void WriteLog(string message) => Logs.Add(message);
        protected override int UtcNowSeconds() => Seconds;
    }

    private PlayerEngine _engine = null!;
    private LocalVariables _vars = null!;
    private InMemoryDBService _db = null!;
    private SlicingService _registry = null!;
    private ScriptedProxy _spawnCalls = null!;
    private ScriptedProxy _guiCalls = null!;
    private ScriptedProxy _itemCalls = null!;
    private bool _windowOpen;
    private TestSlicing _service = null!;

    [SetUp]
    public void SetUp()
    {
        _windowOpen = false;
        _engine = new PlayerEngine();
        _vars = new LocalVariables(_engine.Engine);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid);
        _engine.Engine.SetReturn("GetItemPossessor", Player);
        _engine.Engine.SetReturn("GetNextItemInInventory", ObjectInvalid);

        _db = new InMemoryDBService();
        var dbPlayer = new Player(PlayerEngine.PlayerId);
        dbPlayer.Skills[SkillType.Espionage] = new PlayerSkill { Rank = 3 };
        _db.With(dbPlayer);

        _registry = new SlicingService();
        var spawn = ScriptedProxy.Create<ISpawnService>(out _spawnCalls);
        _spawnCalls.On("DespawnAndQueueRespawn", _ => true);
        var gui = ScriptedProxy.Create<IGuiService>(out _guiCalls);
        _guiCalls.On("IsWindowOpen", _ => _windowOpen);
        _guiCalls.On("CloseWindow", _ => null);
        var item = ScriptedProxy.Create<IItemService>(out _itemCalls);
        _itemCalls.On("ReduceItemStack", _ => true);
        var cache = ScriptedProxy.Create<ICacheService>(out var cacheCalls);
        cacheCalls.On("GetItemNameByResref", args => "name of " + args[0]);

        _service = new TestSlicing(_db, _registry, item, gui, spawn, cache);
        _vars.ClearCalls();
    }

    [TearDown]
    public void TearDown()
    {
        Random.ResetSeed();
        _engine.Dispose();
    }

    private static SlicingBoard CircuitBoard(int middleOrientation, int baseTrace = 5) => new()
    {
        Tier = 1,
        BoardNumber = 1,
        Width = 3,
        Height = 1,
        BaseTrace = baseTrace,
        Tiles =
        {
            new SlicingTile { Type = SlicingTileType.Entry, Orientation = 1 },
            new SlicingTile { Type = SlicingTileType.Straight, Orientation = middleOrientation },
            new SlicingTile { Type = SlicingTileType.Core, Orientation = 3 }
        }
    };

    private ActiveSlicingSession StartOn(SlicingBoard board, int tier = 1)
    {
        _service.TryStart(Player, Target, SlicingSourceType.Terminal, tier, out var error).Should().BeTrue(error);
        var session = _service.Get(Player)!;
        session.Board = board;
        session.TraceRemaining = board.BaseTrace;
        return session;
    }

    [Test]
    public void The_registry_keeps_one_attempt_per_player_id_until_it_is_removed()
    {
        var session = new ActiveSlicingSession { Player = Player };

        _registry.HasSession("a").Should().BeFalse();
        _registry.GetSession("a").Should().BeNull();
        _registry.SetSession("a", session);
        _registry.HasSession("a").Should().BeTrue();
        _registry.GetSession("a").Should().BeSameAs(session);
        _registry.RemoveSession("a");
        _registry.HasSession("a").Should().BeFalse();
        _registry.RemoveSession("a");
    }

    [Test]
    public void Starting_claims_the_target_and_picks_the_board_from_the_seeded_draw()
    {
        Random.SetSeed(7);
        _service.Lockpicking = 20;
        var count = Slicing.GetBoardCount(1);
        var expectedBoard = Slicing.MapSelectionToBoardNumber(1, new System.Random(7).Next(1, count + 1));

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out var error).Should().BeTrue(error);

        var session = _service.Get(Player)!;
        _vars.Int(Target, SlicingSessionService.BoardNumberVariable).Should().Be(expectedBoard);
        _vars.Int(Target, SlicingSessionService.IntegrityVariable).Should().Be(100);
        _vars.String(Target, SlicingSessionService.OwnerVariable).Should().Be(PlayerEngine.PlayerId);
        _vars.Int(Target, SlicingSessionService.OwnerTimestampVariable).Should().Be(Now);
        session.Board.BoardNumber.Should().Be(expectedBoard);
        session.TraceRemaining.Should().Be(session.Board.BaseTrace + Slicing.GetTraceBonus(5, 1, 20, 0));
        session.PrimedToolItem.Should().Be(ObjectInvalid);
        _registry.GetSession(PlayerEngine.PlayerId).Should().BeSameAs(session);
    }

    [Test]
    public void A_target_with_a_board_number_keeps_it_and_draws_nothing()
    {
        Random.SetSeed(7);
        SetLocalInt(Target, SlicingSessionService.BoardNumberVariable, 4);

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out _).Should().BeTrue();

        _service.Get(Player)!.Board.BoardNumber.Should().Be(4);
        Random.Next(1000).Should().Be(new System.Random(7).Next(1000));
    }

    [Test]
    public void A_target_with_only_the_retired_seed_maps_it_to_a_board()
    {
        SetLocalInt(Target, SlicingSessionService.SeedVariable, 123456);

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 2, out _).Should().BeTrue();

        var expected = Slicing.MapSelectionToBoardNumber(2, 123456);
        _vars.Int(Target, SlicingSessionService.BoardNumberVariable).Should().Be(expected);
    }

    [Test]
    public void Starting_is_refused_without_the_rank_a_target_needs_and_changes_nothing()
    {
        _service.PerkLevel = 1;

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 3, out var error).Should().BeFalse();

        error.Should().Contain("Slicing rank 3 is required");
        _service.Get(Player).Should().BeNull();
        _vars.Has(Target, SlicingSessionService.OwnerVariable).Should().BeFalse();
    }

    [TestCase(false, false, "Only players may begin")]
    [TestCase(true, true, "Only players may begin")]
    public void Starting_is_refused_for_anyone_but_a_player(bool isPc, bool isDm, string expected)
    {
        _engine.Engine.SetReturn("GetIsPC", isPc);
        _engine.Engine.SetReturn("GetIsDM", isDm);

        _service.ValidateStart(Player, Target, SlicingSourceType.Terminal, 1).Should().Contain(expected);
    }

    [Test]
    public void Starting_is_refused_in_combat_for_a_bad_tier_far_away_or_for_a_lockbox_that_left_the_inventory()
    {
        _service.ValidateStart(Player, Target, SlicingSourceType.Terminal, 0).Should().Contain("invalid security profile");
        _service.ValidateStart(Player, ObjectInvalid, SlicingSourceType.Terminal, 1).Should().Contain("no longer available");

        _engine.Engine.SetReturn("GetDistanceBetween", 9f);
        _service.ValidateStart(Player, Target, SlicingSourceType.Terminal, 1).Should().Contain("too far away");

        _engine.Engine.SetReturn("GetItemPossessor", ObjectInvalid);
        _service.ValidateStart(Player, Target, SlicingSourceType.Lockbox, 1).Should().Contain("must remain in your inventory");

        _engine.Engine.SetReturn("GetIsInCombat", true);
        _service.ValidateStart(Player, Target, SlicingSourceType.Terminal, 1).Should().Contain("while in combat");

        _engine.Engine.SetReturn("GetIsInCombat", false);
        _engine.Engine.SetReturn("GetDistanceBetween", 5f);
        _engine.Engine.SetReturn("GetItemPossessor", Player);
        _service.ValidateStart(Player, Target, SlicingSourceType.Terminal, 1).Should().BeEmpty();
    }

    [Test]
    public void A_second_attempt_is_refused_while_one_is_running()
    {
        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out _).Should().BeTrue();

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out var error).Should().BeFalse();

        error.Should().Be("Finish or abort your current slicing attempt first.");
    }

    [Test]
    public void A_target_held_by_another_slicer_is_refused_until_the_claim_times_out()
    {
        SetLocalString(Target, SlicingSessionService.OwnerVariable, "other-player");
        SetLocalInt(Target, SlicingSessionService.OwnerTimestampVariable, Now - 179);
        _registry.SetSession("other-player", new ActiveSlicingSession { Player = 5 });

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out var error).Should().BeFalse();

        error.Should().Be("Another slicer currently has control of this target.");
        _registry.HasSession("other-player").Should().BeTrue();

        _service.Seconds = Now + 1;
        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out _).Should().BeTrue();

        _registry.HasSession("other-player").Should().BeFalse("the timed-out claim loses its session");
        _vars.String(Target, SlicingSessionService.OwnerVariable).Should().Be(PlayerEngine.PlayerId);
    }

    [Test]
    public void A_committed_attempt_that_was_abandoned_costs_the_next_slicer_a_failure_and_clears_the_commitment()
    {
        Random.SetSeed(11);
        SetLocalString(Target, SlicingSessionService.OwnerVariable, "other-player");
        SetLocalInt(Target, SlicingSessionService.OwnerTimestampVariable, Now - 500);
        SetLocalInt(Target, SlicingSessionService.CommittedVariable, 1);

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out _).Should().BeTrue();

        _vars.Int(Target, SlicingSessionService.FailuresVariable).Should().Be(1);
        _vars.Has(Target, SlicingSessionService.CommittedVariable).Should().BeFalse();
        _spawnCalls.Calls.Should().BeEmpty();
        var replay = new System.Random(11);
        replay.Next(100);
        replay.Next(1, Slicing.GetBoardCount(1) + 1);
        Random.Next(1000).Should().Be(replay.Next(1000), "the failure rolled once for destruction, then the board draw, then this one");
    }

    [Test]
    public void An_abandoned_committed_attempt_at_the_last_failure_destroys_the_target_and_refuses_the_start()
    {
        SetLocalString(Target, SlicingSessionService.OwnerVariable, PlayerEngine.PlayerId);
        SetLocalInt(Target, SlicingSessionService.OwnerTimestampVariable, Now - 500);
        SetLocalInt(Target, SlicingSessionService.CommittedVariable, 1);
        SetLocalInt(Target, SlicingSessionService.FailuresVariable, 4);

        _service.TryStart(Player, Target, SlicingSourceType.Terminal, 1, out var error).Should().BeFalse();

        error.Should().Be("The abandoned intrusion destabilizes and destroys the target.");
        _vars.Int(Target, SlicingSessionService.IntegrityVariable).Should().Be(0);
        _spawnCalls.Calls.Select(call => call.Member).Should().Equal("DespawnAndQueueRespawn");
        _service.Get(Player).Should().BeNull();
    }

    [Test]
    public void Selecting_a_tile_checks_the_index_and_the_session()
    {
        _service.SelectTile(Player, 0, out var error).Should().BeFalse();
        error.Should().Be("There is no active slicing attempt.");

        var session = StartOn(CircuitBoard(0));

        _service.SelectTile(Player, 9, out error).Should().BeFalse();
        error.Should().Be("That circuit tile is invalid.");
        _service.SelectTile(Player, 1, out _).Should().BeTrue();
        session.SelectedIndex.Should().Be(1);
    }

    [Test]
    public void A_claim_that_was_taken_away_ends_the_attempt_on_the_next_action()
    {
        StartOn(CircuitBoard(0));
        SetLocalString(Target, SlicingSessionService.OwnerVariable, "someone-else");

        _service.SelectTile(Player, 1, out var error).Should().BeFalse();

        error.Should().Be("Your slicing claim is no longer active.");
        _service.Get(Player).Should().BeNull();
    }

    [Test]
    public void An_action_in_combat_or_after_the_target_is_gone_is_refused()
    {
        StartOn(CircuitBoard(0));
        _engine.Engine.SetReturn("GetIsInCombat", true);

        _service.SelectTile(Player, 1, out var error).Should().BeFalse();
        error.Should().Contain("while in combat");

        _engine.Engine.SetReturn("GetIsInCombat", false);
        _engine.Engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Target && (uint)args[0]! != ObjectInvalid);
        _service.SelectTile(Player, 1, out error).Should().BeFalse();
        error.Should().Be("The slicing target is no longer available.");
        _service.Get(Player).Should().BeNull();
    }

    [Test]
    public void Rotating_needs_a_movable_selected_tile_and_spends_the_trace()
    {
        var session = StartOn(CircuitBoard(1));

        _service.RotateSelected(Player, out var message).Should().BeFalse();
        message.Should().Be("Select a tile first.");

        _service.SelectTile(Player, 0, out _);
        _service.RotateSelected(Player, out message).Should().BeFalse();
        message.Should().Contain("START and GOAL sockets are fixed");

        _service.SelectTile(Player, 1, out _);
        _service.RotateSelected(Player, out message).Should().BeTrue();

        message.Should().Be("Action completed. Trace remaining: 4.");
        session.Board.Tiles[1].Orientation.Should().Be(2);
        session.HasCommitted.Should().BeTrue();
        session.History.Should().HaveCount(1);
        _vars.Int(Target, SlicingSessionService.CommittedVariable).Should().Be(1);
    }

    [Test]
    public void Swapping_needs_adjacent_movable_tiles()
    {
        var session = StartOn(CircuitBoard(0));
        _service.SelectTile(Player, 1, out _);

        _service.SwapSelectedWith(Player, 7, out var message).Should().BeFalse();
        message.Should().Be("That circuit tile is invalid.");

        _service.SwapSelectedWith(Player, 2, out message).Should().BeFalse();
        message.Should().Be("Entry and core sockets cannot be displaced.");
        session.TraceRemaining.Should().Be(5);
    }

    [Test]
    public void Solving_the_board_pays_xp_rolls_the_rewards_in_order_and_removes_the_target()
    {
        Random.SetSeed(21);
        var session = StartOn(CircuitBoard(0));
        _service.SelectTile(Player, 1, out _);
        var replay = new System.Random(21);
        replay.Next(1, Slicing.GetBoardCount(1) + 1);
        var expected = SlicingReward.RollSuccess(
            SlicingSourceType.Terminal, 1, replay.Next(10000), replay.Next(int.MaxValue), replay.Next(int.MaxValue));

        _service.RotateSelected(Player, out var message).Should().BeTrue();

        message.Should().Be("Circuit linked. The security core yields.");
        _service.XpGrants.Should().Equal((Player, 103));
        _engine.Engine.CallsTo("CreateItemOnObject").Select(call => (string)call.Arguments[0]!)
            .Should().Equal(expected.Select(reward => reward.Resref));
        _engine.Messages().Should().Equal(expected.Select(reward => $"You recover {reward.Quantity}x name of {reward.Resref}."));
        _service.Get(Player).Should().BeNull();
        _registry.HasSession(PlayerEngine.PlayerId).Should().BeFalse();
        _vars.Has(Target, SlicingSessionService.OwnerVariable).Should().BeFalse();
        _spawnCalls.Calls.Select(call => call.Member).Should().Equal("DespawnAndQueueRespawn");
        _service.Logs.Should().ContainSingle().Which.Should().Contain("completed Terminal slicing board T1-001");
        Random.Next(1000).Should().Be(replay.Next(1000), "the rewards draw exactly three values");
        session.Source.Should().Be(SlicingSourceType.Terminal);
    }

    [Test]
    public void Solving_a_lockbox_destroys_the_item_instead_of_despawning_it()
    {
        Random.SetSeed(5);
        _service.TryStart(Player, Target, SlicingSourceType.Lockbox, 1, out _).Should().BeTrue();
        var session = _service.Get(Player)!;
        session.Board = CircuitBoard(0);
        _service.SelectTile(Player, 1, out _);

        _service.RotateSelected(Player, out _).Should().BeTrue();

        _engine.Engine.CallsTo("DestroyObject").Select(call => (uint)call.Arguments[0]!).Should().Equal(Target);
        _spawnCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void Running_out_of_trace_fails_the_attempt_with_one_destruction_roll()
    {
        Random.SetSeed(33);
        StartOn(CircuitBoard(1, baseTrace: 1));
        _service.SelectTile(Player, 1, out _);
        var replay = new System.Random(33);
        replay.Next(1, Slicing.GetBoardCount(1) + 1);
        replay.Next(100);

        _service.RotateSelected(Player, out var message).Should().BeFalse();

        message.Should().Be("The trace closes before the route reaches the core.");
        _engine.Messages().Should().Equal("The attempt fails. The next failed attempt has a 10% destruction risk.");
        _vars.Int(Target, SlicingSessionService.FailuresVariable).Should().Be(1);
        _vars.Int(Target, SlicingSessionService.IntegrityVariable).Should().Be(100);
        _service.Get(Player).Should().BeNull();
        _vars.Has(Target, SlicingSessionService.OwnerVariable).Should().BeFalse();
        _service.Logs.Should().ContainSingle().Which.Should().Contain("failed Terminal slicing board T1-001 (failure 1, destroyed=False)");
        Random.Next(1000).Should().Be(replay.Next(1000));
    }

    [Test]
    public void Aborting_before_any_move_releases_the_claim_without_a_failure()
    {
        StartOn(CircuitBoard(0));

        _service.Abort(Player);

        _service.Get(Player).Should().BeNull();
        _vars.Has(Target, SlicingSessionService.OwnerVariable).Should().BeFalse();
        _vars.Int(Target, SlicingSessionService.FailuresVariable).Should().Be(0);
        _service.Abort(Player);
    }

    [Test]
    public void Aborting_after_a_move_counts_as_a_failure()
    {
        Random.SetSeed(2);
        StartOn(CircuitBoard(1));
        _service.SelectTile(Player, 1, out _);
        _service.RotateSelected(Player, out _);

        _service.Abort(Player);

        _service.Get(Player).Should().BeNull();
        _vars.Int(Target, SlicingSessionService.FailuresVariable).Should().Be(1);
        _engine.Messages().Should().ContainSingle().Which.Should().StartWith("The attempt fails.");
        _service.GetIntegrity(Target).Should().Be(100);
    }

    [Test]
    public void The_integrity_of_a_fresh_target_reads_as_whole()
    {
        _service.GetIntegrity(Target).Should().Be(100);
        _service.GetFailures(Target).Should().Be(0);
        SetLocalInt(Target, SlicingSessionService.FailuresVariable, 2);
        _service.GetIntegrity(Target).Should().Be(0);
        SetLocalInt(Target, SlicingSessionService.IntegrityVariable, 75);
        _service.GetIntegrity(Target).Should().Be(75);
    }

    [Test]
    public void A_player_who_leaves_abandons_the_attempt()
    {
        StartOn(CircuitBoard(0));
        _engine.Engine.SetReturn("GetExitingObject", Player);

        _service.OnPlayerExit();

        _service.Get(Player).Should().BeNull();
    }

    [Test]
    public void A_player_who_dies_abandons_the_attempt_and_the_open_window_is_closed()
    {
        StartOn(CircuitBoard(0));
        _engine.Engine.SetReturn("GetLastPlayerDied", Player);
        _windowOpen = true;

        _service.OnPlayerDeath();

        _service.Get(Player).Should().BeNull();
        _guiCalls.Calls.Select(call => call.Member).Should().Equal("IsWindowOpen", "CloseWindow");
        _guiCalls.Calls[1].Arguments.Should().Equal(Player, GuiWindowType.Slicing, Player);
    }

    [Test]
    public void A_player_who_dies_with_the_window_closed_only_abandons_the_attempt()
    {
        StartOn(CircuitBoard(0));
        _engine.Engine.SetReturn("GetLastPlayerDied", Player);

        _service.OnPlayerDeath();

        _guiCalls.Calls.Select(call => call.Member).Should().Equal("IsWindowOpen");
    }

    [Test]
    public void A_tool_is_primed_once_and_a_second_tool_is_refused()
    {
        var session = StartOn(CircuitBoard(0));
        SetLocalInt(ToolItem, SlicingSessionService.ToolTypeVariable, (int)SlicingToolType.TraceFuse);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTierVariable, 1);

        _service.ActivateTool(Player, ToolItem, out var message).Should().BeTrue();

        message.Should().Be("Trace fuse primed. It will be consumed on your first move.");
        session.PrimedTool.Should().Be(SlicingToolType.TraceFuse);
        session.PrimedToolItem.Should().Be(ToolItem);
        _service.ActivateTool(Player, ToolItem, out message).Should().BeFalse();
        message.Should().Be("Only one slicing tool may be used per attempt.");
    }

    [Test]
    public void A_tool_below_the_target_tier_is_refused()
    {
        StartOn(CircuitBoard(0), tier: 2);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTypeVariable, (int)SlicingToolType.TraceFuse);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTierVariable, 1);

        _service.ActivateTool(Player, ToolItem, out var message).Should().BeFalse();

        message.Should().Be("That tool cannot penetrate this security tier.");
    }

    [Test]
    public void The_trace_fuse_adds_trace_and_is_spent_on_the_first_move()
    {
        var session = StartOn(CircuitBoard(1));
        SetLocalInt(ToolItem, SlicingSessionService.ToolTypeVariable, (int)SlicingToolType.TraceFuse);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTierVariable, 1);
        _service.ActivateTool(Player, ToolItem, out _);
        _service.SelectTile(Player, 1, out _);

        _service.RotateSelected(Player, out var message).Should().BeTrue();

        message.Should().Be("Action completed. Trace remaining: 5.");
        session.PrimedTool.Should().Be(SlicingToolType.Invalid);
        _itemCalls.Calls.Should().ContainSingle().Which.Arguments.Should().Equal(ToolItem, 1);
    }

    [Test]
    public void The_servo_key_aligns_the_selected_tile_and_solves_the_board()
    {
        Random.SetSeed(9);
        var board = CircuitBoard(2);
        board.Tiles[1].SolutionOrientation = 1;
        var session = StartOn(board);
        _service.SelectTile(Player, 1, out _);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTypeVariable, (int)SlicingToolType.ReversibleServoKey);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTierVariable, 1);

        _service.ActivateTool(Player, ToolItem, out var message).Should().BeTrue();

        message.Should().Be("Circuit linked. The security core yields.");
        board.Tiles[1].Orientation.Should().Be(1);
        _itemCalls.Calls.Should().ContainSingle();
        _service.Get(Player).Should().BeNull();
        session.PrimedToolItem.Should().Be(ObjectInvalid);
    }

    [Test]
    public void Only_tools_that_reach_the_tier_are_offered()
    {
        StartOn(CircuitBoard(0), tier: 2);
        _engine.Engine.SetReturn("GetFirstItemInInventory", ToolItem);
        _engine.Engine.SetReturn("GetName", "Fuse");
        SetLocalInt(ToolItem, SlicingSessionService.ToolTypeVariable, (int)SlicingToolType.TraceFuse);
        SetLocalInt(ToolItem, SlicingSessionService.ToolTierVariable, 2);

        var tools = _service.GetEligibleTools(Player);

        tools.Should().ContainSingle();
        tools[0].Item.Should().Be(ToolItem);
        tools[0].Name.Should().Be("Fuse");
        tools[0].Type.Should().Be(SlicingToolType.TraceFuse);

        SetLocalInt(ToolItem, SlicingSessionService.ToolTierVariable, 1);
        _service.GetEligibleTools(Player).Should().BeEmpty();
    }

    [Test]
    public void No_tools_are_offered_without_an_attempt()
    {
        _service.GetEligibleTools(Player).Should().BeEmpty();
    }
}
