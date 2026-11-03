using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// Unit tests of the droid equipment diagnostics instance: the engine and the events plugin are recording fakes
/// (with real local variables), and the log and the managed stack are answered by a subclass, so the entries the
/// service writes can be read exactly.
/// </summary>
[TestFixture]
public class DroidEquipmentDiagnosticsServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint DroidObject = 100;
    private const uint MasterObject = 101;
    private const uint Rifle = 201;
    private const uint Pistol = 202;
    private const uint AreaObject = 300;
    private const uint TargetObject = 400;
    private const string EventItemTag = "item-hex";
    private const string Stack = "SWLOR.Game.Server.Feature.TwinPistolEquipment.Revalidate";

    private Bench _bench = null!;
    private RecordingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bench = new Bench();
        _service = new RecordingService();
    }

    [TearDown]
    public void TearDown()
    {
        _bench.Dispose();
    }

    // ---- equips

    [Test]
    public void OnEquipped_WhenTheItemIsNowInASlot_WritesTheEntryWithEveryField()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;

        _service.OnEquipped();

        const string expected =
            "change=equipped droid=R2-X (64) master=Zun item=oph_rifle slot=RightHand hands=oph_rifle#C9| " +
            "action=AttackObject inCombat=True target=Mynock associateCommand=7 busy=Invalid area=tat_cantina " +
            "stack=Feature.TwinPistolEquipment.Revalidate";
        _service.LastEntry.Should().Be(expected);
        _service.Logs.Should().Equal((LogGroup.DroidEquipment, expected));
        _bench.Events.CallsTo("GetEventData").Should().OnlyContain(call => (string)call.Arguments[0]! == "ITEM");
    }

    [Test]
    public void OnEquipped_RemembersTheHandsSoTheHeartbeatDoesNotRepeatTheChange()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.OnEquipped();
        _service.Logs.Clear();

        _service.CheckHands();

        _service.Logs.Should().BeEmpty("the equip already moved the baseline to the hands it left");
    }

    [Test]
    public void OnEquipped_IgnoresCreaturesThatAreNotPlayerDroids()
    {
        _bench.ResRefs[DroidObject] = "some_other_creature";
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;

        _service.OnEquipped();

        _service.LastEntry.Should().BeEmpty();
        _service.Logs.Should().BeEmpty();
        _bench.Events.CountOf("GetEventData").Should().Be(0, "a creature that is not a droid never reads the event data");
    }

    [Test]
    public void OnEquipped_WhenTheItemIsStillOutsideEverySlot_IsARefusedEquipAndNotLogged()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Pistol;

        _service.OnEquipped();

        _service.Logs.Should().BeEmpty();
    }

    [Test]
    public void OnEquipped_WhileTheSummonRestoresTheDroid_IsNotLogged()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _bench.Engine.SetLocalBool(DroidObject, "DROID_IS_SPAWNING", true);

        _service.OnEquipped();

        _service.Logs.Should().BeEmpty();
        _service.LastEntry.Should().BeEmpty();
    }

    [Test]
    public void OnEquipped_WhenTheEngineFailsMidEntry_LogsTheErrorAndKeepsGoing()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _bench.Engine.SetReturnHandler("GetCurrentAction", _ => throw new InvalidOperationException("engine went away"));

        var equip = () => _service.OnEquipped();

        equip.Should().NotThrow();
        _service.Logs.Should().Equal((LogGroup.Error, "Droid equipment diagnostics failed: engine went away"));
    }

    [Test]
    public void TheEntryLeavesOutTheItemFieldsWhenNoItemIsNamedAndTheStackWhenNoGameCodeCalled()
    {
        _service.Stack = string.Empty;
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _service.RememberHands(DroidObject);
        _bench.Slots[InventorySlot.RightHand] = Pistol;

        _service.CheckHands();

        _service.LastEntry.Should().NotContain("item=").And.NotContain("slot=");
        _service.LastEntry.Should().EndWith(" stack=");
    }

    // ---- unequips

    [Test]
    public void OnUnequipping_ThenOnUnequipped_WritesTheSlotTheItemLeft()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.OnUnequipping();
        _bench.Slots.Remove(InventorySlot.RightHand);

        _service.OnUnequipped();

        _service.LastEntry.Should().StartWith("change=unequipped droid=R2-X (64) master=Zun item=oph_rifle slot=RightHand hands=|");
        _service.Logs.Should().ContainSingle();
    }

    [Test]
    public void OnUnequipped_WithoutTheBeforeEvent_DoesNotKnowTheSlot()
    {
        _bench.EventItem = Rifle;

        _service.OnUnequipped();

        _service.LastEntry.Should().StartWith("change=unequipped droid=R2-X (64) master=Zun item=oph_rifle slot=Invalid ");
    }

    [Test]
    public void OnUnequipped_AfterTheBeforeEventOfAnotherItem_DoesNotTakeThatSlot()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.OnUnequipping();
        _bench.Slots.Remove(InventorySlot.RightHand);
        _bench.EventItem = Pistol;

        _service.OnUnequipped();

        _service.LastEntry.Should().Contain("item=pistol_a slot=Invalid ");
    }

    [Test]
    public void OnUnequipped_ConsumesTheNotedSlot()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.OnUnequipping();
        _bench.Slots.Remove(InventorySlot.RightHand);
        _service.OnUnequipped();

        _service.OnUnequipped();

        _service.LastEntry.Should().Contain("slot=Invalid ", "the noted slot was used by the first after-event");
    }

    [Test]
    public void OnUnequipped_WhenTheItemIsStillInASlot_WasNotUnequippedAndIsNotLogged()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.OnUnequipping();

        _service.OnUnequipped();

        _service.Logs.Should().BeEmpty();
    }

    [Test]
    public void OnUnequipped_ForACreatureThatIsNotADroid_IsNotLogged()
    {
        _bench.ResRefs[DroidObject] = "some_other_creature";
        _bench.EventItem = Rifle;

        _service.OnUnequipped();

        _service.Logs.Should().BeEmpty();
    }

    [Test]
    public void OnUnequipping_IgnoresCreaturesThatAreNotPlayerDroids()
    {
        _bench.ResRefs[DroidObject] = "some_other_creature";
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.OnUnequipping();
        _bench.ResRefs[DroidObject] = "pc_droid";
        _bench.Slots.Remove(InventorySlot.RightHand);

        _service.OnUnequipped();

        _service.LastEntry.Should().Contain("slot=Invalid ", "nothing was noted for the creature that was not a droid");
    }

    [Test]
    public void OnUnequipping_WhenTheEngineFails_LogsTheErrorAndDoesNotThrow()
    {
        _bench.Engine.SetReturnHandler("GetResRef", _ => throw new InvalidOperationException("no resref"));

        var before = () => _service.OnUnequipping();

        before.Should().NotThrow();
        _service.Logs.Should().Equal((LogGroup.Error, "Droid equipment diagnostics failed: no resref"));
    }

    // ---- the heartbeat

    [Test]
    public void CheckHands_FirstSeesTheHandsSilentlyThenWritesWhenTheyChangeWithoutAnEvent()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _service.CheckHands();
        _service.CheckHands();
        _service.Logs.Should().BeEmpty("the first look only records the baseline and an unchanged look writes nothing");

        _bench.Slots[InventorySlot.RightHand] = Pistol;
        _service.CheckHands();

        _service.Logs.Should().ContainSingle();
        _service.LastEntry.Should().StartWith(
            "change=hands changed without an equip event droid=R2-X (64) master=Zun detail=before=oph_rifle#C9| hands=pistol_a#CA|");

        _service.CheckHands();
        _service.Logs.Should().ContainSingle("the change moved the baseline");
    }

    [Test]
    public void CheckHands_SkipsCreaturesThatAreNotDroidsAndDroidsThatAreStillSpawning()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _service.CheckHands();
        _bench.Slots[InventorySlot.RightHand] = Pistol;

        _bench.ResRefs[DroidObject] = "some_other_creature";
        _service.CheckHands();
        _bench.ResRefs[DroidObject] = "pc_droid";
        _bench.Engine.SetLocalBool(DroidObject, "DROID_IS_SPAWNING", true);
        _service.CheckHands();

        _service.Logs.Should().BeEmpty();
    }

    [Test]
    public void CheckHands_WhenTheEngineFails_LogsTheErrorAndDoesNotThrow()
    {
        _bench.Engine.SetReturnHandler("GetItemInSlot", _ => throw new InvalidOperationException("no slot"));

        var heartbeat = () => _service.CheckHands();

        heartbeat.Should().NotThrow();
        _service.Logs.Should().Equal((LogGroup.Error, "Droid equipment diagnostics failed: no slot"));
    }

    // ---- remembering and forgetting

    [Test]
    public void RememberHands_SetsTheBaselineWithoutLoggingTheRestore()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _service.RememberHands(DroidObject);
        _service.Logs.Should().BeEmpty();

        _service.CheckHands();
        _service.Logs.Should().BeEmpty("the hands are the ones the summon restored");

        _bench.Slots[InventorySlot.LeftHand] = Pistol;
        _service.CheckHands();
        _service.LastEntry.Should().Contain("detail=before=oph_rifle#C9| hands=oph_rifle#C9|pistol_a#CA");
    }

    [Test]
    public void RememberHands_WhenTheEngineFails_LogsTheErrorAndDoesNotThrow()
    {
        _bench.Engine.SetReturnHandler("GetItemInSlot", _ => throw new InvalidOperationException("no slot"));

        var remember = () => _service.RememberHands(DroidObject);

        remember.Should().NotThrow();
        _service.Logs.Should().Equal((LogGroup.Error, "Droid equipment diagnostics failed: no slot"));
    }

    [Test]
    public void Forget_DropsTheBaselineAndTheNotedSlotOfTheDestroyedObject()
    {
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;
        _service.CheckHands();
        _service.OnUnequipping();

        _service.Forget();
        _bench.Slots[InventorySlot.RightHand] = Pistol;
        _service.CheckHands();
        _bench.Slots.Remove(InventorySlot.RightHand);
        _service.OnUnequipped();

        _service.Logs.Should().ContainSingle("the heartbeat has no baseline left to compare with");
        _service.LastEntry.Should().Contain("change=unequipped").And.Contain("slot=Invalid ");
    }

    // ---- the real log and the real stack

    [Test]
    public void WithoutASubclass_TheEntryCarriesTheSummaryOfTheRealStack()
    {
        var plain = new DroidEquipmentDiagnosticsService();
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;

        plain.OnEquipped();

        plain.LastEntry.Should().StartWith("change=equipped droid=R2-X (64)");
        plain.LastEntry.Should().EndWith("stack=", "every frame of this stack is the test, the service or the test runner");
    }

    // ---- the stack summary and the field format

    [Test]
    public void SummarizeStack_KeepsCallingGameCodeAndDropsPlumbing()
    {
        const string stack = """
               at System.Environment.get_StackTrace()
               at SWLOR.Game.Server.Domain.Companions.DroidEquipmentDiagnosticsService.GetStackTrace()
               at SWLOR.Game.Server.Domain.Companions.DroidEquipmentDiagnosticsService.Record(String change, UInt32 droid, UInt32 item, String slot) in C:\x\DroidEquipmentDiagnosticsService.cs:line 40
               at SWLOR.Game.Server.Domain.Companions.DroidEquipmentDiagnosticsService.OnUnequipped() in C:\x\DroidEquipmentDiagnosticsService.cs:line 33
               at SWLOR.Game.Server.Service.DroidEquipmentDiagnostics.OnUnequipped() in C:\x\DroidEquipmentDiagnostics.cs:line 12
               at SWLOR.Game.Server.Core.ScriptDispatcher.Run(String script)
               at SWLOR.NWN.API.NWNX.CreaturePlugin.RunUnequip(UInt32 creature, UInt32 item)
               at SWLOR.Game.Server.Feature.TwinPistolEquipmentValidation.ReturnEquippedItem(UInt32 creature, UInt32 item, InventorySlot slot) in C:\x\T.cs:line 116
               at SWLOR.Game.Server.Feature.TwinPistolEquipmentValidation.ReturnEquippedItem(UInt32 creature, UInt32 item, InventorySlot slot) in C:\x\T.cs:line 117
               at SWLOR.Game.Server.Feature.TwinPistolEquipmentValidation.<>c__DisplayClass.<Revalidate>b__0()
               at NWN.Core.NWScript.AssignCommand(UInt32 obj, Action action)
            """;

        _service.SummarizeStack(stack).Should().Be(
            "Feature.TwinPistolEquipmentValidation.ReturnEquippedItem<" +
            "Feature.TwinPistolEquipmentValidation.<>c__DisplayClass.<Revalidate>b__0");
    }

    [Test]
    public void SummarizeStack_IsEmptyWhenNoGameCodeStartedTheChange()
    {
        const string stack = """
               at System.Environment.get_StackTrace()
               at SWLOR.Game.Server.Service.DroidEquipmentDiagnostics.OnUnequipped()
               at SWLOR.Game.Server.Core.ScriptDispatcher.Run(String script)
            """;

        _service.SummarizeStack(stack).Should().BeEmpty();
        _service.SummarizeStack(null!).Should().BeEmpty();
        _service.SummarizeStack("   ").Should().BeEmpty();
    }

    [Test]
    public void SummarizeStack_KeepsAtMostEightFrames()
    {
        var lines = new List<string>();
        for (var index = 0; index < 12; index++)
            lines.Add($"   at SWLOR.Game.Server.Feature.Caller.Method{index}()");

        _service.SummarizeStack(string.Join("\n", lines)).Split('<').Should().HaveCount(8);
    }

    [Test]
    public void Format_WritesFieldsInOrder()
    {
        var fields = new List<(string, string)> { ("change", "unequipped"), ("item", "oph_rifle"), ("stack", "") };

        _service.Format(fields).Should().Be("change=unequipped item=oph_rifle stack=");
    }

    // ---- the facade

    [Test]
    public void TheFacadeHandlersAndLastEntryWorkOnTheContainersInstance()
    {
        using var container = new FacadeContainer();
        using var scope = container.Install();
        _bench.Slots[InventorySlot.RightHand] = Rifle;
        _bench.EventItem = Rifle;

        DroidEquipmentDiagnostics.OnEquipped();

        DroidEquipmentDiagnostics.LastEntry.Should().StartWith("change=equipped droid=R2-X (64) master=Zun item=oph_rifle");
        container.Get<IDroidEquipmentDiagnosticsService>().LastEntry.Should().Be(DroidEquipmentDiagnostics.LastEntry);
        DroidEquipmentDiagnostics.Format(new[] { ("a", "1") }).Should().Be("a=1");
    }

    /// <summary>The service with the log and the managed stack answered by the test.</summary>
    private sealed class RecordingService : DroidEquipmentDiagnosticsService
    {
        public List<(LogGroup Group, string Details)> Logs { get; } = new();

        public string Stack { get; set; } = "   at SWLOR.Game.Server.Feature.TwinPistolEquipment.Revalidate()";

        protected override string GetStackTrace() => Stack;

        protected override void WriteLog(LogGroup group, string details) => Logs.Add((group, details));
    }

    /// <summary>
    /// A player droid whose resref, name, master, hands and combat state the engine fake answers, with real local
    /// variables, and an events plugin fake that names <see cref="EventItem"/> as the item of the event.
    /// </summary>
    private sealed class Bench : IDisposable
    {
        private readonly IDisposable _engineScope;
        private readonly IDisposable _eventsScope;

        public Bench()
        {
            Engine = new FakeNWScript();
            Events = new FakeEventsPlugin();
            Locals = new LocalVariables(Engine);
            ResRefs = new Dictionary<uint, string>
            {
                [DroidObject] = "pc_droid",
                [Rifle] = "oph_rifle",
                [Pistol] = "pistol_a"
            };
            Names = new Dictionary<uint, string>
            {
                [DroidObject] = "R2-X",
                [MasterObject] = "Zun",
                [TargetObject] = "Mynock"
            };

            Engine.SetReturn("OBJECT_SELF", DroidObject);
            Engine.SetReturn("GetMaster", MasterObject);
            Engine.SetReturn("GetArea", AreaObject);
            Engine.SetReturn("GetAttackTarget", TargetObject);
            Engine.SetReturn("GetCurrentAction", ActionType.AttackObject);
            Engine.SetReturn("GetIsInCombat", true);
            Engine.SetReturn("GetLastAssociateCommand", 7);
            Engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
            Engine.SetReturnHandler("GetResRef", arguments => ResRefs.GetValueOrDefault((uint)arguments[0]!, string.Empty));
            Engine.SetReturnHandler("GetName", arguments => Names.GetValueOrDefault((uint)arguments[0]!, string.Empty));
            Engine.SetReturnHandler("GetTag", arguments => (uint)arguments[0]! == AreaObject ? "tat_cantina" : string.Empty);
            Engine.SetReturnHandler("StringToObject", arguments => (string)arguments[0]! == EventItemTag ? EventItem : ObjectInvalid);
            Engine.SetReturnHandler("GetItemInSlot", arguments =>
                (uint)arguments[1]! == DroidObject && Slots.TryGetValue((InventorySlot)arguments[0]!, out var item) ? item : ObjectInvalid);
            Events.SetReturn("GetEventData", EventItemTag);

            _engineScope = NWScriptRuntime.Override(Engine);
            _eventsScope = EventsPluginRuntime.Override(Events);
        }

        public FakeNWScript Engine { get; }
        public FakeEventsPlugin Events { get; }
        public LocalVariables Locals { get; }
        public Dictionary<uint, string> ResRefs { get; }
        public Dictionary<uint, string> Names { get; }

        /// <summary>The item in each slot of the droid; a slot that is not here is empty.</summary>
        public Dictionary<InventorySlot, uint> Slots { get; } = new();

        /// <summary>The item the event data of the current event names.</summary>
        public uint EventItem { get; set; } = ObjectInvalid;

        public void Dispose()
        {
            _eventsScope.Dispose();
            _engineScope.Dispose();
        }
    }
}
