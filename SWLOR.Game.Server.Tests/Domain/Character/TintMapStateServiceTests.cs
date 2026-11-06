using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The tint map state instance: the pending equipment-swap color carries (lineages, revisions, completion) and the
/// material tables with the equipment slot index built from them.
/// </summary>
[TestFixture]
public class TintMapStateServiceTests
{
    private const uint Source = 10;
    private const uint Replacement = 11;
    private const uint SecondReplacement = 12;
    private static readonly TintMapLayerType[] Layers = { TintMapLayerType.Cloth1, TintMapLayerType.Metal1 };

    private TintMapStateService _state = null!;

    [SetUp]
    public void SetUp() => _state = new TintMapStateService();

    [Test]
    public void RegisteringACarryCapturesRevisionsForEachLayerAndTheLayerWideScope()
    {
        var registration = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);

        registration.Lineage.Should().NotBe(Guid.Empty);
        registration.Revisions.Keys.Should().BeEquivalentTo(new[]
        {
            new TintMapStateRevisionScope(TintMapLayerType.Cloth1, AppearanceArmor.Torso),
            new TintMapStateRevisionScope(TintMapLayerType.Cloth1, AppearanceArmor.Invalid),
            new TintMapStateRevisionScope(TintMapLayerType.Metal1, AppearanceArmor.Torso),
            new TintMapStateRevisionScope(TintMapLayerType.Metal1, AppearanceArmor.Invalid),
        });
        registration.Revisions.Values.Should().OnlyContain(revision => revision == 0);
        _state.BelongsToItemColorCarryLineage(Replacement, registration.Lineage).Should().BeTrue();
        _state.BelongsToItemColorCarryLineage(Source, registration.Lineage).Should().BeFalse();
    }

    [Test]
    public void ASecondCarryFromTheReplacementJoinsTheSameLineage()
    {
        var first = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);
        var second = _state.RegisterPendingItemColorCarry(Replacement, SecondReplacement, AppearanceArmor.Torso, Layers);

        second.Lineage.Should().Be(first.Lineage);
        _state.BelongsToItemColorCarryLineage(SecondReplacement, first.Lineage).Should().BeTrue();
    }

    [Test]
    public void LinkingAReplacementAddsItToTheLineageOfItsSource()
    {
        var registration = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);

        _state.LinkPendingItemColorCarryReplacement(Replacement, SecondReplacement);

        _state.BelongsToItemColorCarryLineage(SecondReplacement, registration.Lineage).Should().BeTrue();
    }

    [Test]
    public void LinkingFromAnItemWithoutALineageDoesNothing()
    {
        var registration = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);

        _state.LinkPendingItemColorCarryReplacement(999, SecondReplacement);

        _state.BelongsToItemColorCarryLineage(SecondReplacement, registration.Lineage).Should().BeFalse();
    }

    [Test]
    public void AnEditOnTheLayerMakesTheOlderCarryStale()
    {
        var registration = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);
        _state.PendingItemColorCarryLayerIsCurrent(
            registration.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso, registration.Revisions)
            .Should().BeTrue();

        _state.MarkPendingItemColorEdit(Replacement, TintMapLayerType.Cloth1, AppearanceArmor.Torso);

        _state.PendingItemColorCarryLayerIsCurrent(
            registration.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso, registration.Revisions)
            .Should().BeFalse();
        _state.PendingItemColorCarryLayerIsCurrent(
            registration.Lineage, TintMapLayerType.Metal1, AppearanceArmor.Torso, registration.Revisions)
            .Should().BeTrue("the edit touched another layer");
    }

    [Test]
    public void AGlobalEditMakesTheCarryOfEveryPartStale()
    {
        var registration = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);

        _state.MarkPendingItemColorEdit(Replacement, TintMapLayerType.Cloth1, AppearanceArmor.Invalid);

        _state.PendingItemColorCarryLayerIsCurrent(
            registration.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso, registration.Revisions)
            .Should().BeFalse();
    }

    [Test]
    public void AnEditOnAnItemWithoutALineageIsIgnored()
    {
        var registration = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);

        _state.MarkPendingItemColorEdit(999, TintMapLayerType.Cloth1, AppearanceArmor.Torso);

        _state.PendingItemColorCarryLayerIsCurrent(
            registration.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso, registration.Revisions)
            .Should().BeTrue();
    }

    [Test]
    public void AnUnknownLineageIsNeverCurrent()
    {
        _state.PendingItemColorCarryLayerIsCurrent(
            Guid.NewGuid(), TintMapLayerType.Cloth1, AppearanceArmor.Torso,
            new Dictionary<TintMapStateRevisionScope, int>()).Should().BeFalse();
    }

    [Test]
    public void TheLineageIsForgottenOnlyWhenEveryPendingCarryCompleted()
    {
        var first = _state.RegisterPendingItemColorCarry(Source, Replacement, AppearanceArmor.Torso, Layers);
        _state.RegisterPendingItemColorCarry(Replacement, SecondReplacement, AppearanceArmor.Torso, Layers);

        _state.CompletePendingItemColorCarry(first.Lineage);
        _state.BelongsToItemColorCarryLineage(Replacement, first.Lineage).Should().BeTrue("one carry is still pending");

        _state.CompletePendingItemColorCarry(first.Lineage);
        _state.BelongsToItemColorCarryLineage(Replacement, first.Lineage).Should().BeFalse();
        _state.BelongsToItemColorCarryLineage(SecondReplacement, first.Lineage).Should().BeFalse();
        _state.PendingItemColorCarryLayerIsCurrent(
            first.Lineage, TintMapLayerType.Cloth1, AppearanceArmor.Torso, first.Revisions).Should().BeFalse();
    }

    [Test]
    public void CompletingAnUnknownLineageDoesNothing()
    {
        var act = () => _state.CompletePendingItemColorCarry(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Test]
    public void MaterialsAreEmptyUntilTheyAreLoaded()
    {
        _state.GetMaterials("pfh0_chest001").Should().BeEmpty();
        _state.GetMaterials(null).Should().BeEmpty();
        _state.GetMaterials(" ").Should().BeEmpty();
    }

    [Test]
    public void LoadedMaterialsAreFoundByModelIgnoringCaseAndReplacedByTheNextLoad()
    {
        var torso = new TintMapMaterialDefinition("Torso", "torso", TintMapLayerType.Cloth1);
        _state.LoadMaterials(new Dictionary<string, List<TintMapMaterialDefinition>>
        {
            ["pfh0_chest001"] = new() { torso },
        });

        _state.GetMaterials("PFH0_CHEST001").Should().ContainSingle().Which.Should().BeSameAs(torso);

        _state.LoadMaterials(new Dictionary<string, List<TintMapMaterialDefinition>>());

        _state.GetMaterials("pfh0_chest001").Should().BeEmpty();
    }

    [Test]
    public void TheEquipmentSlotIndexIsBuiltFromTheLoadedMaterials()
    {
        var layers = new[] { TintMapLayerType.Cloth1 };
        _state.IsEquipmentMaterialExclusiveToArmorPart("torso", TintMapLayerType.Cloth1, AppearanceArmor.Torso)
            .Should().BeFalse("nothing is loaded");

        _state.LoadMaterials(new Dictionary<string, List<TintMapMaterialDefinition>>
        {
            ["pfh0_chest001"] = new() { new TintMapMaterialDefinition("Torso", "torso", layers) },
            ["pfh0_chest002"] = new() { new TintMapMaterialDefinition("Torso", "torso", layers) },
        });

        _state.IsEquipmentMaterialExclusiveToArmorPart("torso", TintMapLayerType.Cloth1, AppearanceArmor.Torso)
            .Should().BeTrue();
        _state.IsEquipmentMaterialExclusiveToArmorPart("torso", TintMapLayerType.Cloth1, AppearanceArmor.Neck)
            .Should().BeFalse();
        _state.AreEquipmentMaterialSlotsEquivalent("torso", "pfh0_chest002", "torso", TintMapLayerType.Cloth1)
            .Should().BeTrue();
        _state.GetEquivalentEquipmentMaterialResrefs("pfh0_chest001", "torso", TintMapLayerType.Cloth1)
            .Should().Contain("torso");
    }
}
