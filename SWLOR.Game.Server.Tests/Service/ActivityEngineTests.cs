using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Service;

/// <summary>
/// Example of testing game code that calls the engine: the engine is swapped for a recording fake, so the test sees exactly
/// which engine calls the game function made, and no game engine is needed.
/// </summary>
public class ActivityEngineTests
{
    private const uint Player = 1001;

    /// <summary>Checks that marking a target busy writes the busy flag and the activity type as local variables.</summary>
    [Test]
    public void SetBusy_WritesTheFlagAndTheType()
    {
        var engine = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(engine);

        Activity.SetBusy(Player, ActivityStatusType.Resting);

        engine.Calls.Select(call => call.ToString()).Should().Equal(
            "SetLocalBool(1001, \"IS_BUSY\", True)",
            "SetLocalInt(1001, \"BUSY_TYPE\", 3)");
    }

    /// <summary>Checks that the busy type reads back as invalid without asking for the type when the target is not busy.</summary>
    [Test]
    public void GetBusyType_IsInvalidWhenTheTargetIsNotBusy()
    {
        var engine = new FakeNWScript();
        using var scope = NWScriptRuntime.Override(engine);

        Activity.GetBusyType(Player).Should().Be(ActivityStatusType.Invalid);

        engine.CountOf("GetLocalInt").Should().Be(0);
    }

    /// <summary>Checks that the busy type is read from the local variable once the target is busy.</summary>
    [Test]
    public void GetBusyType_ReadsTheStoredTypeWhenBusy()
    {
        var engine = new FakeNWScript();
        engine.SetReturn("GetLocalBool", true);
        engine.SetReturn("GetLocalInt", (int)ActivityStatusType.Fishing);
        using var scope = NWScriptRuntime.Override(engine);

        Activity.GetBusyType(Player).Should().Be(ActivityStatusType.Fishing);
    }

    /// <summary>Checks that entering the module clears the busy state of the player the engine reports as entering.</summary>
    [Test]
    public void WipeStatusOnEntry_ClearsTheEnteringPlayer()
    {
        var engine = new FakeNWScript();
        engine.SetReturn("GetEnteringObject", Player);
        using var scope = NWScriptRuntime.Override(engine);

        Activity.WipeStatusOnEntry();

        engine.Calls.Select(call => call.ToString()).Should().Equal(
            "GetEnteringObject()",
            "DeleteLocalBool(1001, \"IS_BUSY\")",
            "DeleteLocalInt(1001, \"BUSY_TYPE\")");
    }
}
