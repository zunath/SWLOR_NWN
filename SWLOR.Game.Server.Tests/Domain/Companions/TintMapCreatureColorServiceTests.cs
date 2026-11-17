using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>A creature's own colour layers: what is stored, saved to the player record and refreshed.</summary>
[TestFixture]
public class TintMapCreatureColorServiceTests
{
    private const uint Creature = 10;
    private const uint Item = 11;

    private TintMapWorld _world = null!;
    private FacadeContainer _container = null!;
    private IDisposable _installed = null!;
    private InMemoryDBService _db = null!;
    private ScriptedProxy _applyCalls = null!;
    private ScriptedProxy _droidOverrideCalls = null!;
    private TintMapCreatureColorService _service = null!;
    private Player _player = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _container = new FacadeContainer();
        _installed = _container.Install();
        _world.Add(Creature, ObjectType.Creature, player: true);
        _world.Add(Item, ObjectType.Item);
        _player = new Player("uuid-10");
        _db = new InMemoryDBService().With(_player);
        var apply = ScriptedProxy.Create<ITintMapApplyService>(out _applyCalls);
        _applyCalls.On("RefreshAfterColorChange", _ => null).On("ApplyCurrentColorsAndPublish", _ => null);
        var droidOverrides = ScriptedProxy.Create<ITintMapDroidOverrideService>(out _droidOverrideCalls);
        _droidOverrideCalls.On("SaveDroidOverrides", _ => null).On("RemoveDroidOverrides", _ => null);
        _service = new TintMapCreatureColorService(apply, _db, droidOverrides);
    }

    [TearDown]
    public void TearDown()
    {
        _installed.Dispose();
        _container.Dispose();
        _world.Dispose();
    }

    private static TintMapMaterialSelection Selection(uint creaturePalette, TintMapLayerType layer, string material = "mat") =>
        new("model", new TintMapMaterialDefinition("Material", material, layer), creaturePalette, creaturePalette,
            usesItemColors: false, AppearanceArmor.Invalid);

    [Test]
    public void SetCreatureCustomColor_StoresTheStateAndEveryOwnedMaterialThenSavesAndRefreshes()
    {
        var color = new TintMapColor(10, 20, 30);
        var material = TintMapVariable.GetName("mat", TintMapLayerType.Hair);
        var state = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair);

        _service.SetCreatureCustomColor(Creature, new[] { Selection(Creature, TintMapLayerType.Hair) },
            TintMapLayerType.Hair, color);

        _world.Int(Creature, state).Should().Be(color.ToStoredValue());
        _world.Int(Creature, material).Should().Be(color.ToStoredValue());
        _droidOverrideCalls.Calls.Should().ContainSingle(call => call.Member == "SaveDroidOverrides"
            && ((IReadOnlyList<string>)call.Arguments[1]!).Contains(material)
            && (int)call.Arguments[2]! == color.ToStoredValue());
        _db.Saved.Should().ContainSingle().Which.Should().BeSameAs(_player);
        _player.CreatureTintOverrides.Should().Contain(state, color.ToStoredValue());
        _applyCalls.Calls.Should().ContainSingle(call => call.Member == "RefreshAfterColorChange");
    }

    [Test]
    public void SetCreatureCustomColor_SkipsSelectionsOwnedByAnotherObjectAndNonCreatureLayers()
    {
        var other = Selection(Item, TintMapLayerType.Hair, "other");

        _service.SetCreatureCustomColor(Creature, new[] { other }, TintMapLayerType.Hair, new TintMapColor(1, 2, 3));
        _service.SetCreatureCustomColor(Creature, new[] { other }, TintMapLayerType.Cloth1, new TintMapColor(1, 2, 3));
        _service.SetCreatureCustomColor(Creature, null!, TintMapLayerType.Hair, new TintMapColor(1, 2, 3));
        _service.SetCreatureCustomColor(999, new[] { other }, TintMapLayerType.Hair, new TintMapColor(1, 2, 3));

        _world.LocalsOf(Creature).Keys.Should().BeEquivalentTo(
            new[] { TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair) });
        _applyCalls.Calls.Count(call => call.Member == "RefreshAfterColorChange").Should().Be(1);
    }

    [Test]
    public void ResetCreatureCustomColor_RemovesTheLayersLocalsAndRecordsTheClear()
    {
        var material = TintMapVariable.GetName("mat", TintMapLayerType.Skin);
        var state = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
        var hair = TintMapVariable.GetName("mat", TintMapLayerType.Hair);
        _world.SetInt(Creature, material, 5);
        _world.SetInt(Creature, state, 6);
        _world.SetInt(Creature, hair, 7);

        _service.ResetCreatureCustomColor(Creature, TintMapLayerType.Skin);

        _world.LocalsOf(Creature).Keys.Should().BeEquivalentTo(new[] { hair });
        _droidOverrideCalls.Calls.Should().ContainSingle(call => call.Member == "RemoveDroidOverrides");
        _player.CreatureTintOverrides.Should().BeEquivalentTo(new Dictionary<string, int> { [hair] = 7 });
        _applyCalls.Calls.Should().ContainSingle(call => call.Member == "ApplyCurrentColorsAndPublish");
    }

    [Test]
    public void ResetCreatureCustomColor_IgnoresAnInvalidCreatureAndAnItemLayer()
    {
        _service.ResetCreatureCustomColor(999, TintMapLayerType.Skin);
        _service.ResetCreatureCustomColor(Creature, TintMapLayerType.Cloth1);

        _applyCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void CarryStoredCreatureCustomColors_ReappliesEveryStoredLayerColor()
    {
        var color = new TintMapColor(9, 8, 7);
        _world.SetInt(Creature, TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin), color.ToStoredValue());

        _service.CarryStoredCreatureCustomColors(Creature);

        _applyCalls.Calls.Should().ContainSingle(call => call.Member == "RefreshAfterColorChange");
    }

    [Test]
    public void CarryStoredCreatureCustomColors_DoesNothingWithoutStoredColorsOrForAnInvalidCreature()
    {
        _service.CarryStoredCreatureCustomColors(Creature);
        _service.CarryStoredCreatureCustomColors(999);

        _applyCalls.Calls.Should().BeEmpty();
    }

    [Test]
    public void RestorePlayerOverrides_ReplacesStaleLocalsWithTheSavedRecord()
    {
        var hairState = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Hair);
        var skinState = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
        _world.SetInt(Creature, skinState, 123);
        _player.CreatureTintOverrides = new Dictionary<string, int>
        {
            [hairState] = 55,
            [TintMapVariable.GetName("mat", TintMapLayerType.Cloth1)] = 66,
            [TintMapVariable.GetName("mat", TintMapLayerType.Hair)] = 0
        };

        _service.RestorePlayerOverrides(Creature);

        _world.LocalsOf(Creature).Should().BeEquivalentTo(new Dictionary<string, int> { [hairState] = 55 });
    }

    [Test]
    public void RestorePlayerOverrides_LeavesLocalsAloneWithoutARecordOrForANonPlayer()
    {
        var skinState = TintMapVariable.GetCreatureColorStateName(TintMapLayerType.Skin);
        _world.SetInt(Creature, skinState, 123);

        _service.RestorePlayerOverrides(Creature);
        _world.Add(12, ObjectType.Creature);
        _world.SetInt(12, skinState, 5);
        _service.RestorePlayerOverrides(12);

        _world.Int(Creature, skinState).Should().Be(123);
        _world.Int(12, skinState).Should().Be(5);
    }
}
