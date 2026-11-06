using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>
/// The appearance render state instance: the held head scales, the model catalogs, the armor part projections with
/// their prune rules, and the preview picture cache. The two engine questions (is an object valid, does a resource
/// exist) are answered by a subclass.
/// </summary>
[TestFixture]
public class AppearanceRenderStateServiceTests
{
    private sealed class TestState : AppearanceRenderStateService
    {
        public HashSet<uint> Invalid { get; } = new();
        public List<string> ResourceQueries { get; } = new();
        public HashSet<string> Resources { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override bool IsObjectValid(uint obj) => !Invalid.Contains(obj);

        protected override bool ResourceExists(string resref)
        {
            ResourceQueries.Add(resref);
            return Resources.Contains(resref);
        }
    }

    private TestState _state = null!;

    [SetUp]
    public void SetUp() => _state = new TestState();

    [Test]
    public void HeadScalesAreHeldPerCreatureAndReleasedOnce()
    {
        _state.HasBaseHeadScale(1).Should().BeFalse();
        _state.TryGetBaseHeadScale(1, out _).Should().BeFalse();
        var missing = () => _state.GetBaseHeadScale(1);
        missing.Should().Throw<KeyNotFoundException>();

        _state.SetBaseHeadScale(1, 1.25f);
        _state.SetBaseHeadScale(1, 1.5f);

        _state.HasBaseHeadScale(1).Should().BeTrue();
        _state.GetBaseHeadScale(1).Should().Be(1.5f);
        _state.TryGetBaseHeadScale(1, out var scale).Should().BeTrue();
        scale.Should().Be(1.5f);
        _state.RemoveBaseHeadScale(1, out var removed).Should().BeTrue();
        removed.Should().Be(1.5f);
        _state.RemoveBaseHeadScale(1, out _).Should().BeFalse();
    }

    [Test]
    public void HeadScalesOfInvalidCreaturesAreDroppedOnlyOnceTheTableIsLarge()
    {
        for (uint creature = 1; creature < 1024; creature++)
            _state.SetBaseHeadScale(creature, 1f);
        _state.Invalid.Add(5);

        _state.PruneBaseHeadScales();
        _state.HasBaseHeadScale(5).Should().BeTrue("the table is below the prune size");

        _state.SetBaseHeadScale(5000, 1f);
        _state.PruneBaseHeadScales();

        _state.HasBaseHeadScale(5).Should().BeFalse();
        _state.HasBaseHeadScale(6).Should().BeTrue();
    }

    [Test]
    public void CatalogsStartEmptyAndAreReplacedWhenSet()
    {
        _state.GetNeckCatalog().Should().NotBeNull();
        _state.GetPartSkinCatalog().Should().NotBeNull();
        _state.GetShoulderCatalog().Should().NotBeNull();
        _state.GetRobeCatalog().Should().NotBeNull();

        var neck = new NeckModelCatalog(Array.Empty<(string, string, string)>());
        var partSkin = new PartSkinModelCatalog(Array.Empty<(string, string)>(), Array.Empty<(string, string, string)>());
        var shoulder = new ShoulderModelCatalog(Array.Empty<(string, string, string)>());
        var robe = new RobeModelCatalog(Array.Empty<(string, int, int)>());
        _state.SetNeckCatalog(neck);
        _state.SetPartSkinCatalog(partSkin);
        _state.SetShoulderCatalog(shoulder);
        _state.SetRobeCatalog(robe);

        _state.GetNeckCatalog().Should().BeSameAs(neck);
        _state.GetPartSkinCatalog().Should().BeSameAs(partSkin);
        _state.GetShoulderCatalog().Should().BeSameAs(shoulder);
        _state.GetRobeCatalog().Should().BeSameAs(robe);
    }

    [Test]
    public void NeckProjectionsAreStoredAndRemovedPerCreature()
    {
        _state.TryGetNeckProjection(1, out var none).Should().BeFalse();
        none.Should().Be(default((uint, ushort, ushort)));

        _state.StoreNeckProjection(1, (Item: 7u, Source: (ushort)3, Render: (ushort)1003));

        _state.TryGetNeckProjection(1, out var projection).Should().BeTrue();
        projection.Should().Be((7u, (ushort)3, (ushort)1003));
        _state.RemoveNeckProjection(1);
        _state.TryGetNeckProjection(1, out _).Should().BeFalse();
    }

    [Test]
    public void StoringANeckProjectionDropsInvalidCreaturesOnceTheTableIsLarge()
    {
        for (uint creature = 1; creature <= 1024; creature++)
            _state.StoreNeckProjection(creature, (creature, 1, 2));
        _state.Invalid.Add(10);

        _state.StoreNeckProjection(5000, (1u, 1, 2));

        _state.TryGetNeckProjection(10, out _).Should().BeFalse();
        _state.TryGetNeckProjection(11, out _).Should().BeTrue();
        _state.TryGetNeckProjection(5000, out _).Should().BeTrue();
    }

    [Test]
    public void PartSkinProjectionsAreKeyedByCreatureAndPart()
    {
        _state.StorePartSkinProjection(1, AppearanceArmor.Torso, (7u, 3, 1003));

        _state.TryGetPartSkinProjection(1, AppearanceArmor.Torso, out var projection).Should().BeTrue();
        projection.Render.Should().Be(1003);
        _state.TryGetPartSkinProjection(1, AppearanceArmor.Belt, out _).Should().BeFalse();
        _state.RemovePartSkinProjection(1, AppearanceArmor.Belt).Should().BeFalse();
        _state.RemovePartSkinProjection(1, AppearanceArmor.Torso).Should().BeTrue();
        _state.TryGetPartSkinProjection(1, AppearanceArmor.Torso, out _).Should().BeFalse();
    }

    [Test]
    public void PartSkinProjectionsArePrunedAtTheirPruneSizeAndThePruneSizeGrowsPastTheLivePopulation()
    {
        for (uint creature = 1; creature <= 1023; creature++)
            _state.StorePartSkinProjection(creature, AppearanceArmor.Torso, (creature, 1, 2));
        _state.Invalid.Add(10);
        _state.PrunePartSkinProjections();
        _state.TryGetPartSkinProjection(10, AppearanceArmor.Torso, out _).Should().BeTrue("below the prune size");

        _state.StorePartSkinProjection(5000, AppearanceArmor.Torso, (1u, 1, 2));
        _state.PrunePartSkinProjections();
        _state.TryGetPartSkinProjection(10, AppearanceArmor.Torso, out _).Should().BeFalse();

        // 1023 live projections now: the next prune waits for twice that.
        _state.Invalid.Add(11);
        for (uint creature = 6000; creature < 6000 + 500; creature++)
            _state.StorePartSkinProjection(creature, AppearanceArmor.Torso, (creature, 1, 2));
        _state.PrunePartSkinProjections();
        _state.TryGetPartSkinProjection(11, AppearanceArmor.Torso, out _).Should().BeTrue();
    }

    [Test]
    public void ShoulderProjectionsAreKeyedByCreatureAndPartAndPrunedWhenLarge()
    {
        _state.StoreShoulderProjection(1, AppearanceArmor.LeftShoulder, (7u, 3, 1003));
        _state.TryGetShoulderProjection(1, AppearanceArmor.LeftShoulder, out var projection).Should().BeTrue();
        projection.Source.Should().Be(3);
        _state.TryGetShoulderProjection(1, AppearanceArmor.RightShoulder, out _).Should().BeFalse();
        _state.RemoveShoulderProjection(1, AppearanceArmor.LeftShoulder);
        _state.TryGetShoulderProjection(1, AppearanceArmor.LeftShoulder, out _).Should().BeFalse();

        for (uint creature = 1; creature <= 2047; creature++)
            _state.StoreShoulderProjection(creature, AppearanceArmor.LeftShoulder, (creature, 1, 2));
        _state.Invalid.Add(20);
        _state.PruneShoulderProjections();
        _state.TryGetShoulderProjection(20, AppearanceArmor.LeftShoulder, out _).Should().BeTrue("below the prune size");

        _state.StoreShoulderProjection(9000, AppearanceArmor.LeftShoulder, (1u, 1, 2));
        _state.PruneShoulderProjections();

        _state.TryGetShoulderProjection(20, AppearanceArmor.LeftShoulder, out _).Should().BeFalse();
        _state.TryGetShoulderProjection(21, AppearanceArmor.LeftShoulder, out _).Should().BeTrue();
    }

    [Test]
    public void APictureLookupIsAnsweredByTheResourceOnceAndThenCachedIgnoringCase()
    {
        _state.Resources.Add("apmh0_chest148f");

        _state.PictureExists("apmh0_chest148f").Should().BeTrue();
        _state.PictureExists("APMH0_CHEST148F").Should().BeTrue();
        _state.PictureExists("apmh0_chest149f").Should().BeFalse();
        _state.PictureExists("apmh0_chest149f").Should().BeFalse();

        _state.ResourceQueries.Should().Equal("apmh0_chest148f", "apmh0_chest149f");
    }
}
