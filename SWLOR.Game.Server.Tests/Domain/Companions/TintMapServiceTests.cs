using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>The tint refresh queue and the event handlers that start it.</summary>
[TestFixture]
public class TintMapServiceTests
{
    private const uint Creature = 10;
    private const uint Npc = 11;
    private const uint Player = 12;
    private const uint Item = 13;
    private const uint Area = 14;

    private TintMapWorld _world = null!;
    private ScriptedProxy _apply = null!;
    private ScriptedProxy _carry = null!;
    private ScriptedProxy _creatureColor = null!;
    private ScriptedProxy _gui = null!;
    private TintMapService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _world = new TintMapWorld();
        _world.Add(Creature, ObjectType.Creature, player: true);
        _world.Add(Npc, ObjectType.Creature);
        _world.Add(Player, ObjectType.Creature, player: true);
        _world.Add(Item, ObjectType.Item);
        _world.Add(Area, ObjectType.Placeable);
        var apply = ScriptedProxy.Create<ITintMapApplyService>(out _apply);
        _apply.On("ApplyCurrentColors", _ => null).On("ApplyCurrentItemColors", _ => null);
        var carry = ScriptedProxy.Create<ITintMapCarryService>(out _carry);
        _carry.On("CarryStoredEquipmentCustomColors", _ => null);
        var creatureColor = ScriptedProxy.Create<ITintMapCreatureColorService>(out _creatureColor);
        _creatureColor.On("RestorePlayerOverrides", _ => null).On("CarryStoredCreatureCustomColors", _ => null);
        var gui = ScriptedProxy.Create<IGuiService>(out _gui);
        _gui.On("PublishRefreshEvent", _ => null);
        _service = new TintMapService(apply, carry, creatureColor, gui);
    }

    [TearDown]
    public void TearDown() => _world.Dispose();

    private void Self(uint creature) => _world.Engine.SetReturn("OBJECT_SELF", creature);

    private int Applied(uint creature, bool reset) => _apply.Calls.Count(call => call.Member == "ApplyCurrentColors"
        && (uint)call.Arguments[0]! == creature && (bool)call.Arguments[1]! == reset);

    [Test]
    public void OnModuleEnter_RestoresAPlayersColorsBeforeTheDelayedRefresh()
    {
        _world.Engine.SetReturn("GetEnteringObject", Creature);

        _service.OnModuleEnter();

        _creatureColor.Calls.Should().ContainSingle(call => call.Member == "RestorePlayerOverrides");
        Applied(Creature, reset: false).Should().Be(0, "the refresh is delayed so the restored locals exist first");
        _world.RunDelayed();
        Applied(Creature, reset: false).Should().Be(1);
    }

    [Test]
    public void OnModuleEnter_IgnoresAnObjectThatIsNotAPlayer()
    {
        _world.Engine.SetReturn("GetEnteringObject", Npc);

        _service.OnModuleEnter();

        _creatureColor.Calls.Should().BeEmpty();
        _world.Engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void QueueRefresh_RunsTheStoredColorCarriesThenAppliesTheColors()
    {
        _service.QueueRefresh(Creature, resetShaderOverrides: false);
        _world.Engine.CallsTo("DelayCommand").Should().ContainSingle();
        _apply.Calls.Should().BeEmpty("the refresh waits for the engine to finish the change that queued it");

        _world.RunDelayed();

        _carry.Calls.Should().ContainSingle(call => call.Member == "CarryStoredEquipmentCustomColors");
        _creatureColor.Calls.Should().ContainSingle(call => call.Member == "CarryStoredCreatureCustomColors");
        Applied(Creature, reset: false).Should().Be(1);
    }

    [Test]
    public void QueueRefresh_SkipsAnInvalidCreatureAndOneThatLeftBeforeTheDelayEnded()
    {
        _service.QueueRefresh(999);
        _world.Engine.CallsTo("DelayCommand").Should().BeEmpty();

        _service.QueueRefresh(Npc);
        _world.Remove(Npc);
        _world.RunDelayed();

        _carry.Calls.Should().BeEmpty();
    }

    [Test]
    public void QueueItemRefresh_AppliesTheItemColorsAfterTheDelay()
    {
        _service.QueueItemRefresh(Item);
        _service.QueueItemRefresh(999);
        _world.RunDelayed();

        _apply.Calls.Should().ContainSingle(call => call.Member == "ApplyCurrentItemColors" && (uint)call.Arguments[0]! == Item);
    }

    [Test]
    public void QueueRefreshAndEditor_AlsoPublishesTheEditorRefreshForAValidPlayer()
    {
        _service.QueueRefreshAndEditor(Npc, Player);
        _world.RunDelayed();

        _gui.Calls.Should().ContainSingle(call => call.Member == "PublishRefreshEvent" && (uint)call.Arguments[0]! == Player
            && call.Arguments[1] is AppearanceChangedRefreshEvent);
        Applied(Npc, reset: true).Should().Be(1);
    }

    [Test]
    public void QueueRefreshAndEditor_WithoutAPlayerOnlyRefreshesTheCreature()
    {
        _service.QueueRefreshAndEditor(Npc, 999);
        _world.RunDelayed();

        _gui.Calls.Should().BeEmpty();
        Applied(Npc, reset: true).Should().Be(1);
    }

    [Test]
    public void OnPlayerSpawnAndOnCreatureSpawn_QueueTheSpawnedCreature()
    {
        Self(Creature);
        _service.OnPlayerSpawn();
        Self(Npc);
        _service.OnCreatureSpawn();
        _world.RunDelayed();

        Applied(Creature, reset: false).Should().Be(1);
        Applied(Npc, reset: true).Should().Be(1);
    }

    [Test]
    public void OnModuleEquipAndOnModuleUnacquire_QueueTheCreatureAndTheLostItem()
    {
        _world.Engine.SetReturn("GetPCItemLastEquippedBy", Creature);
        _world.Engine.SetReturn("GetModuleItemLost", Item);

        _service.OnModuleEquip();
        _service.OnModuleUnacquire();
        _world.RunDelayed();

        Applied(Creature, reset: true).Should().Be(1);
        _apply.Calls.Should().ContainSingle(call => call.Member == "ApplyCurrentItemColors");
    }

    [Test]
    public void OnModuleUnequip_ReleasesAHiddenHelmetAndQueuesTheWearer()
    {
        _world.Engine.SetReturn("GetPCItemLastUnequipped", Item);
        _world.Engine.SetReturn("GetPCItemLastUnequippedBy", Creature);
        _world.SetInt(Item, "HELMET_RENDER_HEAD_HIDDEN", 1);

        _service.OnModuleUnequip();
        _world.RunDelayed();

        Applied(Creature, reset: true).Should().Be(1);
    }

    [Test]
    public void OnAppearanceEquipmentChanged_QueuesAnNpcAndAnItsEditorButNotAPlayer()
    {
        Self(Creature);
        _service.OnAppearanceEquipmentChanged();
        _world.Engine.CallsTo("DelayCommand").Should().BeEmpty("module events cover players");

        Self(Npc);
        _world.Engine.SetReturn("GetMaster", Player);
        _service.OnAppearanceEquipmentChanged();
        _world.RunDelayed();

        Applied(Npc, reset: true).Should().Be(1);
        _gui.Calls.Should().ContainSingle(call => call.Member == "PublishRefreshEvent");
    }

    [Test]
    public void OnAppearanceEdit_AppliesTheEditedCreaturesColors()
    {
        Self(Npc);

        _service.OnAppearanceEdit();

        Applied(Npc, reset: true).Should().Be(1);
    }

    [Test]
    public void OnAreaEnter_QueuesTheEnteringCreatureAndRefreshesTheAreaOnce()
    {
        _world.Engine.SetReturn("GetEnteringObject", Creature);
        _world.Engine.SetReturn("GetArea", Area);

        _service.OnAreaEnter();
        _service.OnAreaEnter();
        _world.RunDelayed();

        _world.Int(Area, "TINT_MAP_COMPAT_REFRESHED").Should().Be(1);
        Applied(Creature, reset: false).Should().Be(2);
    }

    [Test]
    public void OnAreaEnter_IgnoresAnEnteringObjectThatIsNotACreature()
    {
        _world.Engine.SetReturn("GetEnteringObject", Item);

        _service.OnAreaEnter();

        _world.Engine.CallsTo("DelayCommand").Should().BeEmpty();
    }

    [Test]
    public void OnAreaEnter_QueuesAnNpcWithAFullShaderReset()
    {
        _world.Engine.SetReturn("GetEnteringObject", Npc);
        _world.Engine.SetReturn("GetArea", 999);

        _service.OnAreaEnter();
        _world.RunDelayed();

        Applied(Npc, reset: true).Should().Be(1);
    }
}
