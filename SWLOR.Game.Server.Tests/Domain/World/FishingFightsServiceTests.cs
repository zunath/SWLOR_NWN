using System.Numerics;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.FishingService;
using SWLOR.Game.Server.Tests.Service;
using SWLOR.Game.Server.Tests.Service.Line;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using SWLOR.Game.Server.Tests.Support;

namespace SWLOR.Game.Server.Tests.Domain.World;

/// <summary>
/// Unit tests of the fight instance: the lifecycle it owns runs against a fake world and a scripted random (the fight rules
/// themselves are covered by <c>FishingFightLifecycleTests</c>), and the engine is a recording fake for the death and
/// logout handlers.
/// </summary>
[TestFixture]
public class FishingFightsServiceTests : SkillFacadeFixture
{
    private const uint Player = 11;
    private const uint Rod = 22;
    private const uint Point = 33;

    private FakeFishingFightWorld _world = null!;
    private FakeNWScript _engine = null!;
    private IDisposable _engineScope = null!;
    private TestFishingFights _service = null!;

    private sealed class TestFishingFights : FishingFightsService
    {
        public TestFishingFights(IFishingFightWorld world)
            : base(world, new ScriptedMinigameRandom(99, 99, 99, 99, 99, 99, 99, 99))
        {
        }
    }

    [OneTimeSetUp]
    public void LoadTheXpChart()
    {
        using (SkillFacades.Install())
            Skill.CacheXPChartData();
    }

    [SetUp]
    public void SetUp()
    {
        _world = new FakeFishingFightWorld();
        _engine = new FakeNWScript();
        _engineScope = NWScriptRuntime.Override(_engine);
        _service = new TestFishingFights(_world);
    }

    [TearDown]
    public void TearDown() => _engineScope.Dispose();

    private static FishingBite Bite() =>
        new(Player, Rod, Point, new FishAttribute("Test Fish", "test_fish", 0, true, rarity: FishRarity.Common),
            FishingRodType.LuShang, 50, 5, "Little Worm", 7, new Vector3(10f, 20f, 0f));

    [Test]
    public void A_bite_opens_a_fight_that_is_active_until_it_is_abandoned()
    {
        var fight = _service.Bite(Bite());

        fight.Should().NotBeNull();
        _service.IsActive(fight).Should().BeTrue();

        _service.Abandon(fight, FishingFightEnd.WindowClosed);

        _service.IsActive(fight).Should().BeFalse();
    }

    [Test]
    public void No_fight_is_active_and_a_missing_fight_is_abandoned_quietly()
    {
        _service.IsActive(null).Should().BeFalse();
        _service.Invoking(service => service.Abandon(null, FishingFightEnd.Left)).Should().NotThrow();
    }

    [Test]
    public void The_quick_reel_chance_needs_a_fight()
    {
        _service.Invoking(service => service.QuickReelChance(null)).Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void An_angler_who_dies_loses_the_fish_at_once()
    {
        var fight = _service.Bite(Bite());
        _engine.SetReturn("GetLastPlayerDied", Player);

        _service.AbandonOnDeath();

        _service.IsActive(fight).Should().BeFalse();
    }

    [Test]
    public void An_angler_who_logs_out_loses_the_fish_at_once()
    {
        var fight = _service.Bite(Bite());
        _engine.SetReturn("GetExitingObject", Player);

        _service.AbandonOnExit();

        _service.IsActive(fight).Should().BeFalse();
    }

    [Test]
    public void A_death_of_someone_without_a_fight_changes_nothing()
    {
        var fight = _service.Bite(Bite());
        _engine.SetReturn("GetLastPlayerDied", Player + 1);

        _service.AbandonOnDeath();

        _service.IsActive(fight).Should().BeTrue();
    }
}
