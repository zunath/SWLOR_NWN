using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Artisans' Charter on a craft session: the city of the station is recorded when the craft starts, and the XP is scaled when it is
/// settled, for the citizens of that city whose Edict is in effect then.
/// </summary>
public class CraftSessionArtisansCharterTests
{
    private const RecipeType TestRecipe = RecipeType.VenomCoating1;
    private const string City = "city-a";

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
            Components = new Dictionary<string, int> { ["ore"] = 3 },
            EnhancementType = RecipeEnhancementType.Armor,
            EnhancementSlots = 0
        };

        _world = new FakeCraftSessionWorld { StationCityId = City, CharterCityId = City, CharterPoints = 10 };
        _world.Recipes[TestRecipe] = _recipe;
        _world.Inventory["ore"] = 9;

        _player = new Player("player-1");
        _player.Skills[SkillType.Smithery] = new PlayerSkill { Rank = 10 };
    }

    private CraftSessionLifecycle Lifecycle() => new(_world, new ScriptedMinigameRandom(100, 100, 100, 100));

    private CraftSession Start(CraftSessionLifecycle lifecycle)
    {
        var result = lifecycle.Start(FakeCraftSessionWorld.PlayerHandle, _player, new CraftSessionStartRequest(TestRecipe, OBJECT_INVALID, []));
        result.Succeeded.Should().BeTrue(result.Message);
        return result.Session;
    }

    private int BaseXp => CraftMinigameXp.BaseXp(_recipe, 10);

    [Test]
    public void Start_RecordsTheCityOfTheStationOnTheSession()
    {
        Start(Lifecycle());

        _player.ActiveCraftSession.CityId.Should().Be(City);
    }

    [Test]
    public void Start_AStationOutsideAnyCityRecordsNoCity()
    {
        _world.StationCityId = string.Empty;

        Start(Lifecycle());

        _player.ActiveCraftSession.CityId.Should().BeNullOrEmpty();
    }

    [Test]
    public void Succeed_AtAStationInTheCitizensCity_PaysTenPointsMoreXp()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        var scaled = EdictBonusRules.ScaleCraftXp(BaseXp, 10);
        scaled.Should().BeGreaterThan(BaseXp);
        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateXp(scaled, 0, true, 1f)));
    }

    [Test]
    public void Succeed_AtAStationInAnotherCity_PaysTheNormalXp()
    {
        _world.StationCityId = "city-b";
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateXp(BaseXp, 0, true, 1f)));
    }

    [Test]
    public void Succeed_WhenTheEdictIsNotInEffectAtSettlement_PaysTheNormalXp()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);
        _world.CharterPoints = 0;

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateXp(BaseXp, 0, true, 1f)));
    }

    [Test]
    public void Succeed_ForASessionSavedBeforeTheCityWasRecorded_PaysTheNormalXp()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);
        _player.ActiveCraftSession.CityId = null;

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateXp(BaseXp, 0, true, 1f)));
    }

    [Test]
    public void Fail_ScalesTheFailureXpToo_AfterALogoutOrARestart()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);
        lifecycle.RecordActions(_player, session, CraftSessionTestPace.Actions);
        _world.AdvanceByActions(CraftSessionTestPace.Actions);

        lifecycle.Fail(FakeCraftSessionWorld.PlayerHandle, _player, CraftSessionEndReason.Interrupted);

        var scaled = EdictBonusRules.ScaleCraftXp(BaseXp, 10);
        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateFailureXp(scaled, 0)));
    }
}
