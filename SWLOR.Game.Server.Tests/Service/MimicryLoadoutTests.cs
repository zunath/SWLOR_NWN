using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.PlayerMigration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class MimicryLoadoutTests
{
    [Test]
    public void LegacyRecordMigratesEquippedTechniquesIntoAnIndependentDefaultSnapshot()
    {
        var player = JsonConvert.DeserializeObject<Player>("{\"EquippedTechniques\":[2797,2799]}")!;
        var before = player.EquippedTechniques.ToArray();
        var migration = new _20_MimicryLoadouts();
        migration.MigratePlayerData(player);
        var saved = player.MimicryLoadouts.Should().ContainSingle().Subject;
        saved.Name.Should().Be("Default");
        saved.Techniques.Should().Equal(before);
        player.ActiveMimicryLoadoutId.Should().Be(saved.Id);
        player.EquippedTechniques.Clear();
        saved.Techniques.Should().Equal(before, "enforcement must not rewrite the migrated build");
        migration.MigratePlayerData(player);
        player.MimicryLoadouts.Should().ContainSingle().Which.Id.Should().Be(saved.Id);
    }

    [Test]
    public void MigrationPreservesNewerSavedBuildsAndActiveSelection()
    {
        var player = new Player();
        MimicryLoadouts.EnsureDefault(player);
        var id = player.ActiveMimicryLoadoutId;
        MimicryLoadouts.Rename(player, id, "Renamed", out _).Should().BeTrue();
        new _20_MimicryLoadouts().MigratePlayerData(player);
        player.MimicryLoadouts.Should().ContainSingle().Which.Name.Should().Be("Renamed");
        player.ActiveMimicryLoadoutId.Should().Be(id);
    }

    [Test]
    public void NewCharacterGetsAnEmptyDefaultAndSerializedBuildsRoundTrip()
    {
        var player = new Player();
        MimicryLoadouts.EnsureDefault(player).Should().BeTrue();
        player.MimicryLoadouts[0].HotbarSlots[35] = FeatType.FrostSpitTechnique;
        var copy = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;
        copy.MimicryLoadouts.Should().BeEquivalentTo(player.MimicryLoadouts);
        copy.ActiveMimicryLoadoutId.Should().Be(player.ActiveMimicryLoadoutId);
        MimicryLoadouts.EnsureDefault(copy).Should().BeFalse();
    }

    [Test]
    public void SavingCopiesTheDraftWithoutChangingEquippedTechniquesOrCooldowns()
    {
        var player = new Player();
        player.EquippedTechniques.Add(FeatType.FrostSpitTechnique);
        MimicryLoadouts.EnsureDefault(player);
        var draft = new List<FeatType> { FeatType.ToxicSpitTechnique };
        var recasts = player.RecastTimes;
        MimicryLoadouts.Save(player, null, " Poison ", draft, out var saved, out _).Should().BeTrue();
        draft.Clear();
        saved.Name.Should().Be("Poison");
        saved.Techniques.Should().Equal(FeatType.ToxicSpitTechnique);
        player.EquippedTechniques.Should().Equal(FeatType.FrostSpitTechnique);
        player.RecastTimes.Should().BeSameAs(recasts);
        player.ActiveMimicryLoadoutId.Should().Be(player.MimicryLoadouts[0].Id);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("A\nB")]
    [TestCase("1234567890123456789012345")]
    [TestCase("dEfAuLt")]
    public void InvalidOrDuplicateNamesDoNotCreateBuilds(string name)
    {
        var player = new Player();
        MimicryLoadouts.EnsureDefault(player);
        MimicryLoadouts.Save(player, null, name, [], out _, out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
        player.MimicryLoadouts.Should().ContainSingle();
    }

    [Test]
    public void FiveBuildLimitAllowsOverwriteButRejectsSixthBuild()
    {
        var player = new Player();
        MimicryLoadouts.EnsureDefault(player);
        for (var i = 1; i < 5; i++)
            MimicryLoadouts.Save(player, null, $"Build {i}", [], out _, out _).Should().BeTrue();
        MimicryLoadouts.Save(player, null, "Sixth", [], out _, out _).Should().BeFalse();
        MimicryLoadouts.Save(player, player.MimicryLoadouts[0].Id, "Default", [FeatType.FrostSpitTechnique], out _, out _)
            .Should().BeTrue();
        player.MimicryLoadouts.Should().HaveCount(5);
    }

    [Test]
    public void DeleteActivePresetKeepsEquipmentAndLastBuildCannotBeDeleted()
    {
        var player = new Player();
        player.EquippedTechniques.Add(FeatType.FrostSpitTechnique);
        MimicryLoadouts.EnsureDefault(player);
        var id = player.ActiveMimicryLoadoutId;
        MimicryLoadouts.Delete(player, id, out _).Should().BeFalse();
        MimicryLoadouts.Save(player, null, "Other", [], out _, out _).Should().BeTrue();
        MimicryLoadouts.Delete(player, id, out _).Should().BeTrue();
        player.EquippedTechniques.Should().Equal(FeatType.FrostSpitTechnique);
        player.ActiveMimicryLoadoutId.Should().BeEmpty();
        MimicryLoadouts.EnsureDefault(player).Should().BeFalse();
    }

    [Test]
    public void WholeBuildValidationRejectsUnknownUnlearnedDuplicateOverRankAndOverBudgetWithoutMutation()
    {
        Ability.CacheData();
        Mimicry.CacheData();
        var player = new Player();
        player.Perks[PerkType.CombatAnalyzer] = 1;
        player.Skills[SkillType.Mimicry] = new PlayerSkill { Rank = 50 };
        player.LearnedTechniques[FeatType.FrostSpitTechnique] = DateTime.UtcNow;
        player.LearnedTechniques[FeatType.TerrifyingBellowTechnique] = DateTime.UtcNow;
        player.EquippedTechniques.Add(FeatType.FrostSpitTechnique);
        var before = JsonConvert.SerializeObject(player);
        foreach (var invalid in new FeatType[][]
        {
            [FeatType.Invalid], [FeatType.ToxicSpitTechnique],
            [FeatType.FrostSpitTechnique, FeatType.FrostSpitTechnique], [FeatType.TerrifyingBellowTechnique]
        })
        {
            Mimicry.ValidateLoadout(player, invalid, out var error).Should().BeFalse();
            error.Should().NotBeEmpty();
            JsonConvert.SerializeObject(player).Should().Be(before);
        }
        Mimicry.ValidateLoadout(player, [FeatType.FrostSpitTechnique], out _).Should().BeTrue();
        player.Skills[SkillType.Mimicry].Rank = 0;
        Mimicry.ValidateLoadout(player, [FeatType.FrostSpitTechnique], out var rankError).Should().BeFalse();
        rankError.Should().Contain("rank 3");
        Mimicry.ValidateLoadout(player, [], out _).Should().BeTrue();
    }
}
