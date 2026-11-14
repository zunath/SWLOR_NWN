using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Auto-relocking doors when a hot reload has activated a new build (P4-15 closure audit): the close-and-relock timer is a closure
/// the reload drops, so a door the old build left open gets it again.
/// </summary>
[TestFixture]
public class DoorScriptsTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Area = 900;
    private const uint OpenAutoRelockDoor = 901;
    private const uint OpenOrdinaryDoor = 902;
    private const uint ClosedAutoRelockDoor = 903;
    private const string OpenVersion = "AUTO_RELOCK_OPEN_VERSION";

    [Test]
    public void A_reload_starts_the_close_and_relock_timer_again_for_an_auto_relocking_door_left_open()
    {
        var engine = new FakeNWScript();
        var locals = new LocalVariables(engine);
        using var scope = NWScriptRuntime.Override(engine);
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        engine.SetReturn("GetFirstArea", Area);
        engine.SetReturn("GetNextArea", Invalid);
        engine.SetReturn("GetFirstObjectInArea", OpenAutoRelockDoor);
        engine.SetReturnSequence("GetNextObjectInArea", OpenOrdinaryDoor, ClosedAutoRelockDoor, Invalid);
        engine.SetReturnHandler("GetIsOpen", args => (uint)args[0]! != ClosedAutoRelockDoor);
        engine.SetLocalInt(OpenAutoRelockDoor, OpenVersion, 2);
        engine.SetLocalInt(ClosedAutoRelockDoor, OpenVersion, 1);

        DoorScripts.ResumeAutoRelockForReload(Array.Empty<uint>());

        var timer = engine.CallsTo("DelayCommand").Should().ContainSingle("only the open auto-relocking door is waiting to close").Subject;
        timer.Arguments[0].Should().Be(5f);
        locals.Int(OpenAutoRelockDoor, OpenVersion).Should().Be(3, "the new timer replaces any the old build armed");

        ((Action)timer.Arguments[1]!)();

        engine.CallsTo("AssignCommand").Should().ContainSingle().Which.Arguments[0].Should().Be(OpenAutoRelockDoor);
    }
}
