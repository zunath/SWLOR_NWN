using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The creatures a hot reload resets the animation mappings of (P4-15 closure audit): the closures that would have released an NPC's
/// clip, queued swing or suspended pistol remap were dropped, so every creature other than a player that still holds one is reset.
/// Players are reset by their restore step.
/// </summary>
[TestFixture]
public class SavedAnimationMappingsTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Area = 900;
    private const uint Player = 1;
    private const uint ClipNpc = 2;
    private const uint SwingNpc = 3;
    private const uint ThrowingNpc = 4;
    private const uint MappedNpc = 5;
    private const uint IdleNpc = 6;

    [Test]
    public void A_reload_resets_every_creature_but_the_players_that_holds_a_token_a_suspended_remap_or_a_mapping()
    {
        var engine = new FakeNWScript();
        _ = new LocalVariables(engine);
        using var scope = NWScriptRuntime.Override(engine);
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != Invalid);
        engine.SetReturn("GetFirstArea", Area);
        engine.SetReturn("GetNextArea", Invalid);
        engine.SetReturn("GetFirstObjectInArea", Player);
        engine.SetReturnSequence("GetNextObjectInArea", ClipNpc, SwingNpc, ThrowingNpc, MappedNpc, IdleNpc, Invalid);
        engine.SetReturnHandler("GetIsPC", args => (uint)args[0]! == Player);
        engine.SetLocalString(Player, "_NAMED_ANIMATION_PLAYBACK", "token");
        engine.SetLocalString(ClipNpc, "_NAMED_ANIMATION_PLAYBACK", "token");
        engine.SetLocalString(SwingNpc, "_QUEUED_ATTACK_ANIMATION", "token");
        engine.SetLocalInt(ThrowingNpc, "PISTOL_ANIM_THROW_SUSPEND_COUNT", 1);

        var reset = SavedAnimationMappings.FindCreaturesToReset(creature => creature == MappedNpc);

        reset.Should().Equal(ClipNpc, SwingNpc, ThrowingNpc, MappedNpc);
    }
}
