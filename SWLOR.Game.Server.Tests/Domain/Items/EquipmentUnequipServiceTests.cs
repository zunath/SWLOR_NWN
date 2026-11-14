using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Items;

/// <summary>
/// Unit tests of the equipment removal guards. Each test builds its own instance, so no guard registered by the
/// game's equipment owners (or by another test) takes part.
/// </summary>
[TestFixture]
public class EquipmentUnequipServiceTests
{
    private const uint Creature = 42;
    private const uint Item = 84;

    private EquipmentUnequipService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new EquipmentUnequipService();
    }

    [Test]
    public void RegisteringTheSameOwnerReplacesItsGuardWithoutDuplicateDispatch()
    {
        var oldCalls = 0;
        var newCalls = 0;
        _service.RegisterGuard("pair", (_, _) => { oldCalls++; return true; });
        Func<uint, uint, bool> replacement = (actor, item) =>
        {
            actor.Should().Be(Creature);
            item.Should().Be(Item);
            newCalls++;
            return true;
        };
        _service.RegisterGuard("pair", replacement);
        _service.RegisterGuard("pair", replacement);

        _service.TryHandle(Creature, Item).Should().BeTrue();

        oldCalls.Should().Be(0);
        newCalls.Should().Be(1);
    }

    [Test]
    public void UnmatchedRequestsFallThroughAndMatchingOwnersStopDispatch()
    {
        var secondCalls = 0;
        _service.TryHandle(Creature, Item).Should().BeFalse("no guard is registered");
        _service.RegisterGuard("first", (_, item) => item == Item);
        _service.RegisterGuard("second", (_, _) => { secondCalls++; return false; });

        _service.TryHandle(Creature, Item + 1).Should().BeFalse();
        secondCalls.Should().Be(1);
        _service.TryHandle(Creature, Item).Should().BeTrue();
        secondCalls.Should().Be(1, "the first guard handled the request, so the second never ran");
    }

    [Test]
    public void IndependentOwnersCanHandleTheirItemsWithoutBlockingUnrelatedEquipment()
    {
        const uint actor = uint.MaxValue;
        _service.RegisterGuard("first", (creature, item) => creature == actor && item == 1);
        _service.RegisterGuard("second", (creature, item) => creature == actor && item == 2);

        _service.TryHandle(actor, 1).Should().BeTrue();
        _service.TryHandle(actor, 2).Should().BeTrue();
        _service.TryHandle(actor, 3).Should().BeFalse();
        _service.TryHandle(actor - 1, 1).Should().BeFalse();

        _service.RegisterGuard("first", (_, _) => false);
        _service.TryHandle(actor, 1).Should().BeFalse("registration replaces the same owner's previous guard");
        _service.TryHandle(actor, 2).Should().BeTrue("replacing one owner cannot remove another owner's guard");
    }

    [Test]
    public void GuardsAreAskedInRegistrationOrderAndAReplacementKeepsItsPlace()
    {
        var asked = new List<string>();
        _service.RegisterGuard("a", (_, _) => { asked.Add("a"); return false; });
        _service.RegisterGuard("b", (_, _) => { asked.Add("b"); return false; });
        _service.RegisterGuard("a", (_, _) => { asked.Add("a2"); return false; });

        _service.TryHandle(Creature, Item);

        asked.Should().Equal("a2", "b");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void RegisteringWithoutAnOwnerKey_IsRefused(string? key)
    {
        var register = () => _service.RegisterGuard(key!, (_, _) => true);

        register.Should().Throw<ArgumentException>();
    }

    [Test]
    public void RegisteringWithoutAGuard_IsRefused()
    {
        var register = () => _service.RegisterGuard("owner", null!);

        register.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void BeforeUnequip_WhenAGuardHandlesTheItem_SkipsTheEventAndTheSharedScripts()
    {
        using var bench = new EventBench(Creature, Item);
        _service.RegisterGuard("owner", (creature, item) => creature == Creature && item == Item);

        _service.BeforeUnequip();

        bench.Events.CallsTo("GetEventData").Single().Arguments[0].Should().Be("ITEM");
        bench.Events.CountOf("SkipEvent").Should().Be(1);
        bench.Engine.CountOf("ExecuteScript").Should().Be(0);
    }

    [Test]
    public void BeforeUnequip_WhenNoGuardHandlesTheItem_RunsTheSharedUnequipScripts()
    {
        using var bench = new EventBench(Creature, Item);
        _service.RegisterGuard("owner", (_, _) => false);

        _service.BeforeUnequip();

        bench.Events.CountOf("SkipEvent").Should().Be(0);
        var script = bench.Engine.CallsTo("ExecuteScript").Should().ContainSingle().Which;
        script.Arguments[0].Should().Be(ScriptName.OnItemUnequipBefore);
        script.Arguments[1].Should().Be(Creature);
    }

    /// <summary>The creature that fires the event, and the item the event data names.</summary>
    private sealed class EventBench : IDisposable
    {
        private readonly IDisposable _engineScope;
        private readonly IDisposable _eventsScope;

        public EventBench(uint creature, uint item)
        {
            Engine = new FakeNWScript();
            Events = new FakeEventsPlugin();
            Engine.SetReturn("OBJECT_SELF", creature);
            Engine.SetReturnHandler("StringToObject", arguments => (string)arguments[0]! == "item-hex" ? item : 0x7F000000u);
            Events.SetReturn("GetEventData", "item-hex");
            _engineScope = NWScriptRuntime.Override(Engine);
            _eventsScope = EventsPluginRuntime.Override(Events);
        }

        public FakeNWScript Engine { get; }
        public FakeEventsPlugin Events { get; }

        public void Dispose()
        {
            _eventsScope.Dispose();
            _engineScope.Dispose();
        }
    }
}
