using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>A droid's saved tint overrides: which records change and which neighbours are asked.</summary>
[TestFixture]
public class TintMapDroidOverrideServiceTests
{
    private const uint Creature = 10;
    private const uint Controller = 11;
    private const uint Item = 12;

    private TintMapWorld _world = null!;
    private ScriptedProxy _droidCalls = null!;
    private TintMapDroidOverrideService _service = null!;
    private ConstructedDroid _record = null!;
    private int _saves;
    private bool _isDroid;
    private uint _controller;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _world.Add(Creature, ObjectType.Creature);
        _world.Add(Controller, ObjectType.Item);
        _world.Add(Item, ObjectType.Item);
        _record = new ConstructedDroid();
        _saves = 0;
        _isDroid = true;
        _controller = Controller;
        var droid = ScriptedProxy.Create<IDroidService>(out _droidCalls);
        _droidCalls
            .On("IsDroid", _ => _isDroid)
            .On("GetControllerItem", _ => _controller)
            .On("LoadConstructedDroid", _ => _record)
            .On("SaveConstructedDroid", _ =>
            {
                _saves++;
                return null;
            })
            .On("UpdateEquippedItemSnapshot", _ => null);
        _service = new TintMapDroidOverrideService(droid);
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private static TintMapMaterialSelection Selection(uint paletteSource, TintMapLayerType layer) =>
        new("model", new TintMapMaterialDefinition("Material", "material", layer), paletteSource, Creature,
            usesItemColors: true, AppearanceArmor.Invalid);

    [Test]
    public void RemoveDroidOverrides_RemovesTheNamedOverridesAndSavesOnce()
    {
        _record.TintOverrides["TM_a"] = 3;
        _record.TintOverrides["TM_b"] = 4;

        _service.RemoveDroidOverrides(Creature, new[] { "TM_a", "TM_missing" });

        _record.TintOverrides.Should().BeEquivalentTo(new Dictionary<string, int> { ["TM_b"] = 4 });
        _saves.Should().Be(1);
    }

    [Test]
    public void RemoveDroidOverrides_DoesNotSaveWhenNothingChanged()
    {
        _service.RemoveDroidOverrides(Creature, new[] { "TM_missing" });

        _saves.Should().Be(0);
    }

    [Test]
    public void RemoveDroidOverrides_StopsForAnEmptyListANonDroidOrAMissingController()
    {
        _record.TintOverrides["TM_a"] = 3;
        _service.RemoveDroidOverrides(Creature, Array.Empty<string>());
        _isDroid = false;
        _service.RemoveDroidOverrides(Creature, new[] { "TM_a" });
        _isDroid = true;
        _controller = 999;
        _service.RemoveDroidOverrides(Creature, new[] { "TM_a" });

        _record.TintOverrides.Should().ContainKey("TM_a");
        _saves.Should().Be(0);
    }

    [Test]
    public void SaveDroidOverrides_StoresTheColorUnderEveryName()
    {
        _service.SaveDroidOverrides(Creature, new[] { "TM_a", "TM_b" }, 77);

        _record.TintOverrides.Should().BeEquivalentTo(new Dictionary<string, int> { ["TM_a"] = 77, ["TM_b"] = 77 });
        _saves.Should().Be(1);
    }

    [Test]
    public void SaveDroidOverrides_StopsForANonDroidOrAMissingController()
    {
        _isDroid = false;
        _service.SaveDroidOverrides(Creature, new[] { "TM_a" }, 77);
        _isDroid = true;
        _controller = 999;
        _service.SaveDroidOverrides(Creature, new[] { "TM_a" }, 77);
        _service.SaveDroidOverrides(Creature, Array.Empty<string>(), 77);

        _record.TintOverrides.Should().BeEmpty();
        _saves.Should().Be(0);
    }

    [Test]
    public void SaveDroidOverride_OnTheDroidsOwnPaletteStoresOrClearsTheValue()
    {
        var selection = Selection(Creature, TintMapLayerType.Skin);

        _service.SaveDroidOverride(Creature, selection, TintMapLayerType.Skin, "TM_skin", 5);
        _record.TintOverrides["TM_skin"].Should().Be(5);

        _service.SaveDroidOverride(Creature, selection, TintMapLayerType.Skin, "TM_skin", 0);
        _record.TintOverrides.Should().NotContainKey("TM_skin");
        _saves.Should().Be(2);
    }

    [Test]
    public void SaveDroidOverride_OnAnItemPaletteRefreshesTheEquippedItemSnapshot()
    {
        _service.SaveDroidOverride(Creature, Selection(Item, TintMapLayerType.Cloth1), TintMapLayerType.Cloth1, "TM_x", 5);

        _droidCalls.Calls.Should().ContainSingle(call => call.Member == "UpdateEquippedItemSnapshot"
            && (uint)call.Arguments[0]! == Creature && (uint)call.Arguments[1]! == Item);
        _saves.Should().Be(0);
    }

    [Test]
    public void SaveDroidOverride_StopsForANonDroidOrAMissingController()
    {
        var selection = Selection(Creature, TintMapLayerType.Skin);
        _isDroid = false;
        _service.SaveDroidOverride(Creature, selection, TintMapLayerType.Skin, "TM_skin", 5);
        _isDroid = true;
        _controller = 999;
        _service.SaveDroidOverride(Creature, selection, TintMapLayerType.Skin, "TM_skin", 5);

        _saves.Should().Be(0);
    }
}
