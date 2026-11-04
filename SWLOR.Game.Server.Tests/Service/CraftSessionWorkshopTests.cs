using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.EdictService;
using SWLOR.Game.Server.Service.HouseSpecializationService;
using SWLOR.Game.Server.Service.SkillService;
using static SWLOR.NWN.API.NWScript.NWScript;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// The house Workshop on a craft session (Player City Revamp, "House specializations"): the bonus is captured when the craft opens and
/// recorded on the session, so a type change in the house mid-craft, a logout or a restart never changes it, and it stacks with
/// Artisans' Charter, which scales the XP rather than the stats.
/// </summary>
public class CraftSessionWorkshopTests
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

        _world = new FakeCraftSessionWorld { StationCityId = City, CharterCityId = City, CharterPoints = 10, WorkshopBonus = HouseSpecializationRules.WorkshopBonus };
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

    [Test]
    public void Start_RecordsTheWorkshopBonusOfTheHouseTheCraftOpensIn()
    {
        Start(Lifecycle());

        _player.ActiveCraftSession.WorkshopBonus.Should().Be(HouseSpecializationRules.WorkshopBonus);
    }

    [Test]
    public void Start_InAHouseWithoutAWorkshop_RecordsZero_NotNull()
    {
        _world.WorkshopBonus = 0;

        Start(Lifecycle());

        _player.ActiveCraftSession.WorkshopBonus.Should().Be(0, "a recorded zero keeps a Workshop built mid-craft out of it");
    }

    [Test]
    public void ChangingTheHouseMidCraft_DoesNotTouchTheRecordedBonus()
    {
        Start(Lifecycle());

        _world.WorkshopBonus = 0;

        _player.ActiveCraftSession.WorkshopBonus.Should().Be(HouseSpecializationRules.WorkshopBonus);
        HouseSpecializationRules.ResolveWorkshopBonus(_player.ActiveCraftSession, SkillType.Smithery, _world.WorkshopBonus)
            .Should().Be(HouseSpecializationRules.WorkshopBonus);
    }

    [Test]
    public void TheRecordedBonus_SurvivesALogoutOrRestart()
    {
        Start(Lifecycle());

        // The record is saved on the player as JSON; the bonus is read back from it.
        var json = System.Text.Json.JsonSerializer.Serialize(_player.ActiveCraftSession);
        var reloaded = System.Text.Json.JsonSerializer.Deserialize<CraftSessionRecord>(json);

        reloaded.WorkshopBonus.Should().Be(HouseSpecializationRules.WorkshopBonus);
        HouseSpecializationRules.ResolveWorkshopBonus(reloaded, SkillType.Smithery, liveBonus: 0).Should().Be(HouseSpecializationRules.WorkshopBonus);
    }

    [Test]
    public void ARecordSavedBeforeWorkshopsExisted_ReadsAsNoBonus()
    {
        var reloaded = System.Text.Json.JsonSerializer.Deserialize<CraftSessionRecord>("{\"SessionId\":\"old\",\"Skill\":" + (int)SkillType.Smithery + "}");

        reloaded.WorkshopBonus.Should().BeNull();
        HouseSpecializationRules.ResolveWorkshopBonus(reloaded, SkillType.Smithery, liveBonus: 5).Should().Be(0);
    }

    [Test]
    public void TheWorkshop_StacksWithArtisansCharter_BecauseTheCharterPaysXpAndTheWorkshopPaysStats()
    {
        var lifecycle = Lifecycle();
        var session = Start(lifecycle);
        var record = _player.ActiveCraftSession;

        record.CityId.Should().Be(City);
        record.WorkshopBonus.Should().Be(HouseSpecializationRules.WorkshopBonus);

        lifecycle.SucceedAtPace(_world, FakeCraftSessionWorld.PlayerHandle, _player, session, new MinigameMeter(100, 100));

        var baseXp = CraftMinigameXp.BaseXp(_recipe, 10);
        var scaled = EdictBonusRules.ScaleCraftXp(baseXp, 10);
        _world.XpGiven.Should().Equal((SkillType.Smithery, CraftOutcomeRules.CalculateXp(scaled, 0, true, 1f)));
    }
}
