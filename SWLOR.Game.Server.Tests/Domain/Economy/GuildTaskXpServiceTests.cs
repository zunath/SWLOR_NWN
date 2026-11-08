using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Economy;

/// <summary>
/// The guild hand-in XP instance: the skill each guild pays in, the level that sets a task's pay, and the award a player
/// gets or is given. The engine and the player record are fakes, and the unconverted XP chart, skill-XP call and recipe
/// list are answered by a subclass.
/// </summary>
[TestFixture]
public class GuildTaskXpServiceTests
{
    private const uint Player = 1001;

    private IDisposable _scope = null!;
    private InMemoryDBService _db = null!;
    private TestXp _xp = null!;

    [SetUp]
    public void SetUp()
    {
        var engine = new FakeNWScript();
        engine.SetReturn("GetObjectUUID", "uuid-1001");
        _scope = NWScriptRuntime.Override(engine);
        _db = new InMemoryDBService();
        _xp = new TestXp(_db);
    }

    [TearDown]
    public void TearDown() => _scope.Dispose();

    private void PlayerAtRank(SkillType skill, int rank)
    {
        _db.With(new Player
        {
            Id = "uuid-1001",
            Skills = new Dictionary<SkillType, PlayerSkill> { [skill] = new PlayerSkill { Rank = rank } }
        });
    }

    [Test]
    public void TheCraftingGuildsPayInTheirOwnSkillAndTheOthersPayNone()
    {
        _xp.SkillOf(GuildType.SmitheryGuild).Should().Be(SkillType.Smithery);
        _xp.GivesXp(GuildType.SmitheryGuild).Should().BeTrue();
        _xp.GivesXp(GuildType.HuntersGuild).Should().BeFalse();
    }

    [TestCase(10, 10, 1, 300)]
    [TestCase(10, 10, 4, 1200)]
    [TestCase(10, 10, 0, 0)]
    [TestCase(10, 10, -2, 0)]
    public void TheAwardIsHalfTheChartsPayPerItem(int level, int rank, int quantity, int expected)
    {
        _xp.Amount(level, rank, quantity).Should().Be(expected);
    }

    [Test]
    public void ACraftedTaskUsesTheRecipeLevelAndACatchTaskTheFishLevel()
    {
        _xp.Recipes.Add(new RecipeDetail { Resref = "Blade", Skill = SkillType.Smithery, Level = 12, IsActive = true });
        _xp.Recipes.Add(new RecipeDetail { Resref = "blade", Skill = SkillType.Smithery, Level = 8, IsActive = true });
        _xp.Recipes.Add(new RecipeDetail { Resref = "retired", Skill = SkillType.Smithery, Level = 1, IsActive = false });
        _xp.Recipes.Add(new RecipeDetail { Resref = string.Empty, Skill = SkillType.Smithery, Level = 1, IsActive = true });

        _xp.TaskLevel(GuildType.SmitheryGuild, "BLADE", true).Should().Be(8, "the lowest active recipe level, without case");
        _xp.TaskLevel(GuildType.SmitheryGuild, "retired", true).Should().BeNull();
        _xp.TaskLevel(GuildType.SmitheryGuild, "blade", false).Should().Be(8, "a non-fish good a recipe also makes");
        _xp.TaskLevel(GuildType.AgricultureGuild, "moat_carp", false).Should().Be(0, "the fish level");
        _xp.TaskLevel(GuildType.HuntersGuild, "blade", true).Should().BeNull();
        _xp.TaskLevel(GuildType.SmitheryGuild, "", true).Should().BeNull();
        _xp.FishLevel("Frigorifish").Should().Be(50);
        _xp.FishLevel("not_a_fish").Should().BeNull();
    }

    [Test]
    public void TheLookupsAreBuiltOnce()
    {
        _xp.Recipes.Add(new RecipeDetail { Resref = "blade", Skill = SkillType.Smithery, Level = 8, IsActive = true });

        _xp.TaskLevel(GuildType.SmitheryGuild, "blade", true);
        _xp.TaskLevel(GuildType.SmitheryGuild, "blade", true);

        _xp.RecipeReads.Should().Be(1);
    }

    [Test]
    public void ThePlayersAwardComesFromTheirRankInTheGuildsSkill()
    {
        _xp.Recipes.Add(new RecipeDetail { Resref = "blade", Skill = SkillType.Smithery, Level = 10, IsActive = true });
        PlayerAtRank(SkillType.Smithery, 10);

        _xp.Calculate(Player, GuildType.SmitheryGuild, "blade", 2, true).Should().Be(600);
        _xp.Calculate(Player, GuildType.HuntersGuild, "blade", 2, true).Should().Be(0, "the guild pays no XP");
        _xp.Calculate(Player, GuildType.SmitheryGuild, "unknown", 2, true).Should().Be(0, "no level is known");
    }

    [Test]
    public void APlayerWithNoRecordOrNoRankInTheSkillGetsNothing()
    {
        _xp.Recipes.Add(new RecipeDetail { Resref = "blade", Skill = SkillType.Smithery, Level = 10, IsActive = true });

        _xp.Calculate(Player, GuildType.SmitheryGuild, "blade", 1, true).Should().Be(0);

        PlayerAtRank(SkillType.Engineering, 10);
        _xp.Calculate(Player, GuildType.SmitheryGuild, "blade", 1, true).Should().Be(0);
    }

    [Test]
    public void GivingTheXpGoesThroughTheSkillServiceWithoutBonusesSkippedOrPenalties()
    {
        _xp.Recipes.Add(new RecipeDetail { Resref = "blade", Skill = SkillType.Smithery, Level = 10, IsActive = true });
        PlayerAtRank(SkillType.Smithery, 10);

        _xp.Give(Player, GuildType.SmitheryGuild, "blade", 1, true);
        _xp.Give(Player, GuildType.SmitheryGuild, "unknown", 1, true);

        _xp.Given.Should().Equal((Player, SkillType.Smithery, 300));
    }

    private sealed class TestXp : GuildTaskXpService
    {
        public TestXp(IDBService db)
            : base(db, AmbientServices.Of<ICraftService>(), AmbientServices.Of<ISkillService>())
        {
        }

        public List<RecipeDetail> Recipes { get; } = new();
        public List<(uint Player, SkillType Skill, int Xp)> Given { get; } = new();
        public int RecipeReads { get; private set; }

        protected override int GetDeltaXP(int delta) => Math.Max(0, 600 - Math.Abs(delta) * 75);

        protected override void GiveSkillXP(uint player, SkillType skill, int xp) => Given.Add((player, skill, xp));

        protected override IEnumerable<RecipeDetail> GetAllRecipes()
        {
            RecipeReads++;
            return Recipes;
        }
    }
}
