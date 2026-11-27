using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Service;

public class MimicryLoadoutTests
{
    // The code under test reaches converted services through their static facades, which resolve from a container.
    private FacadeContainer _facades = null!;
    private IDisposable _facadeScope = null!;

    [OneTimeSetUp]
    public void BuildFacades() => _facades = new FacadeContainer();

    [OneTimeTearDown]
    public void DisposeFacades() => _facades.Dispose();

    [SetUp]
    public void InstallFacades() => _facadeScope = _facades.Install();

    [TearDown]
    public void RemoveFacades() => _facadeScope.Dispose();

    [Test]
    public void NewCharacterGetsAnEmptyDefaultAndSerializedBuildsRoundTrip()
    {
        var player = new Player();
        MimicryLoadouts.EnsureStartingBuild(player).Should().BeTrue();
        player.MimicryLoadouts[0].HotbarSlots[35] = FeatType.FrostSpitTechnique;
        var copy = JsonConvert.DeserializeObject<Player>(JsonConvert.SerializeObject(player))!;
        copy.MimicryLoadouts.Should().BeEquivalentTo(player.MimicryLoadouts);
        copy.ActiveMimicryLoadoutId.Should().Be(player.ActiveMimicryLoadoutId);
        MimicryLoadouts.EnsureStartingBuild(copy).Should().BeFalse();
    }

    [Test]
    public void SavingCopiesTheDraftWithoutChangingEquippedTechniquesOrCooldowns()
    {
        var player = new Player();
        player.EquippedTechniques.Add(FeatType.FrostSpitTechnique);
        MimicryLoadouts.EnsureStartingBuild(player);
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
        MimicryLoadouts.EnsureStartingBuild(player);
        MimicryLoadouts.Save(player, null, name, [], out _, out var error).Should().BeFalse();
        error.Should().NotBeEmpty();
        player.MimicryLoadouts.Should().ContainSingle();
    }

    [Test]
    public void FiveBuildLimitAllowsOverwriteButRejectsSixthBuild()
    {
        var player = new Player();
        MimicryLoadouts.EnsureStartingBuild(player);
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
        MimicryLoadouts.EnsureStartingBuild(player);
        var id = player.ActiveMimicryLoadoutId;
        MimicryLoadouts.Delete(player, id, out _).Should().BeFalse();
        MimicryLoadouts.Save(player, null, "Other", [], out _, out _).Should().BeTrue();
        MimicryLoadouts.Delete(player, id, out _).Should().BeTrue();
        player.EquippedTechniques.Should().Equal(FeatType.FrostSpitTechnique);
        player.ActiveMimicryLoadoutId.Should().BeEmpty();
        MimicryLoadouts.EnsureStartingBuild(player).Should().BeFalse();
    }
}
