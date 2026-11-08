using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// Quick craft batches: the checks before one starts, the batch tracking and the cancel and interrupt paths. The batch itself
/// runs on an in-memory world that completes an item each time its scheduled step is run.
/// </summary>
[TestFixture]
public class CraftQuickCraftServiceTests
{
    private const RecipeType TestRecipe = RecipeType.VenomCoating1;
    private const uint Handle = 1001;

    private sealed class QuickWorld : IQuickCraftWorld
    {
        public List<Action> Steps { get; } = new();
        public int Created { get; private set; }

        public bool IsPlayerAvailable(uint player) => true;
        public void BeginBatch(uint player) { }
        public void PlayStationWork(uint player, SkillType skill) { }
        public void EndBatch(uint player) { }
        public void PlayItemAnimation(uint player, float seconds) { }
        public bool HasComponents(uint player, RecipeDetail recipe) => true;

        public bool CreateItem(uint player, RecipeDetail recipe)
        {
            Created++;
            return true;
        }

        public bool TryConsumeComponents(uint player, RecipeDetail recipe) => true;
        public void RemoveCreatedItem(uint player, RecipeDetail recipe) { }
        public void LogCraft(uint player, string text) { }
        public void Schedule(float seconds, Action action) => Steps.Add(action);
        public void Message(uint player, string text) { }
    }

    private sealed class TestQuickCraft : CraftQuickCraftService
    {
        public TestQuickCraft(IDBService db, ICraftService craft, ICraftSessionsService sessions, ICraftMinigameRoutingService routing)
            : base(db, craft, sessions, routing, AmbientServices.Of<ISkillService>())
        {
        }

        public QuickWorld World { get; } = new();
        public List<string> Logs { get; } = new();

        protected override QuickCraftBatch CreateBatch(uint player, RecipeDetail recipe, int quantity) => new(World, player, recipe, quantity);

        protected override string GetSkillName(SkillType skill) => "Smithery";

        protected override void WriteLog(string message) => Logs.Add(message);
    }

    private PlayerEngine _engine = null!;
    private InMemoryDBService _db = null!;
    private Player _player = null!;
    private RecipeDetail _recipe = null!;
    private bool _crafting;
    private bool _windowOpen;
    private bool _requirementsMet;
    private TestQuickCraft _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _engine.Engine.SetReturn("GetName", "Tester");
        _db = new InMemoryDBService();
        _recipe = new RecipeDetail
        {
            Resref = "test_item",
            Skill = SkillType.Smithery,
            Level = 10,
            Components = new Dictionary<string, int>()
        };
        _player = new Player(PlayerEngine.PlayerId);
        _player.Skills[SkillType.Smithery] = new PlayerSkill { Rank = 20 };
        _player.CraftedRecipes[TestRecipe] = DateTime.UtcNow;
        _db.With(_player);
        _crafting = false;
        _windowOpen = false;
        _requirementsMet = true;

        var craft = ScriptedProxy.Create<ICraftService>(out var craftScript);
        craftScript.On("GetRecipe", _ => _recipe).On("CanPlayerCraftRecipe", _ => _requirementsMet);
        var sessions = ScriptedProxy.Create<ICraftSessionsService>(out var sessionsScript);
        sessionsScript.On("IsCrafting", _ => _crafting);
        var routing = ScriptedProxy.Create<ICraftMinigameRoutingService>(out var routingScript);
        routingScript.On("IsAnyCraftWindowOpen", _ => _windowOpen);
        _service = new TestQuickCraft(_db, craft, sessions, routing);
    }

    [TearDown]
    public void TearDown() => _engine.Dispose();

    [Test]
    public void Evaluate_refuses_a_player_who_cannot_be_loaded()
    {
        _engine.Engine.SetReturn("GetObjectUUID", "nobody");

        var eligibility = _service.Evaluate(Handle, TestRecipe);

        eligibility.IsEligible.Should().BeFalse();
        eligibility.Reason.Should().Be("Your character could not be loaded.");
    }

    [Test]
    public void Evaluate_names_the_rank_the_recipe_needs()
    {
        _player.Skills[SkillType.Smithery].Rank = 3;

        var eligibility = _service.Evaluate(Handle, TestRecipe);

        eligibility.IsEligible.Should().BeFalse();
        eligibility.Reason.Should().Be($"Quick craft needs Smithery rank {QuickCraftRules.GetRequiredRank(_recipe)}.");
    }

    [Test]
    public void Evaluate_treats_a_skill_the_player_lacks_as_rank_zero_and_asks_for_a_hand_craft_first()
    {
        _player.Skills.Remove(SkillType.Smithery);
        _service.Evaluate(Handle, TestRecipe).Reason.Should().StartWith("Quick craft needs Smithery rank");

        _player.Skills[SkillType.Smithery] = new PlayerSkill { Rank = 20 };
        _player.CraftedRecipes.Clear();
        _service.Evaluate(Handle, TestRecipe).Reason.Should().Be("Craft this recipe by hand once to unlock quick craft.");
    }

    [Test]
    public void Evaluate_refuses_a_recipe_whose_requirements_are_not_met_and_accepts_the_rest()
    {
        _requirementsMet = false;
        _service.Evaluate(Handle, TestRecipe).Reason.Should().Be("Recipe requirements not met.");

        _requirementsMet = true;
        _service.Evaluate(Handle, TestRecipe).Should().Be(QuickCraftEligibility.Eligible);
    }

    [Test]
    public void ResolveBatchSize_is_the_request_when_the_recipe_needs_no_materials_and_is_capped()
    {
        _service.ResolveBatchSize(Handle, TestRecipe, 4).Should().Be(4);
        _service.ResolveBatchSize(Handle, TestRecipe, 400).Should().Be(QuickCraftRules.MaxBatchSize);
    }

    [Test]
    public void Start_refuses_while_another_craft_is_running_or_a_window_is_open()
    {
        _crafting = true;
        _service.Start(Handle, TestRecipe, 2, null!).Should().Be("Finish your current craft first.");

        _crafting = false;
        _windowOpen = true;
        _service.Start(Handle, TestRecipe, 2, null!).Should().Be("Finish your current craft first.");
    }

    [Test]
    public void Start_refuses_a_player_who_is_not_eligible_and_one_with_no_materials()
    {
        _requirementsMet = false;
        _service.Start(Handle, TestRecipe, 2, null!).Should().Be("Recipe requirements not met.");

        _requirementsMet = true;
        _service.Start(Handle, TestRecipe, 0, null!).Should().Be("You do not have the materials for this recipe.");
    }

    [Test]
    public void A_started_batch_runs_to_the_end_and_reports_it()
    {
        var ended = 0;

        _service.Start(Handle, TestRecipe, 2, () => ended++).Should().BeEmpty();

        _service.IsRunning(Handle).Should().BeTrue();
        _service.Start(Handle, TestRecipe, 2, null!).Should().Be("A quick craft is already running.");

        while (_service.IsRunning(Handle))
        {
            var step = _service.World.Steps[^1];
            _service.World.Steps.Clear();
            step();
        }

        ended.Should().Be(1);
        _service.World.Created.Should().Be(2);
        _service.Logs.Should().ContainSingle().Which.Should().Contain("Tester quick crafted 2 of 2").And.Contain("VenomCoating1");
    }

    [Test]
    public void Cancel_and_Interrupt_stop_a_running_batch()
    {
        _service.Start(Handle, TestRecipe, 3, null!);
        _service.Cancel(Handle);
        _service.IsRunning(Handle).Should().BeFalse();
        _service.Logs.Should().ContainSingle().Which.Should().Contain("Cancelled");

        _service.Start(Handle, TestRecipe, 3, null!);
        _service.Interrupt(Handle);
        _service.IsRunning(Handle).Should().BeFalse();
        _service.Logs.Should().HaveCount(2);
        _service.Logs[1].Should().Contain("Interrupted");
    }

    [Test]
    public void Cancel_and_Interrupt_do_nothing_when_no_batch_is_running()
    {
        _service.Invoking(service => service.Cancel(Handle)).Should().NotThrow();
        _service.Invoking(service => service.Interrupt(Handle)).Should().NotThrow();
        _service.IsRunning(Handle).Should().BeFalse();
    }
}
