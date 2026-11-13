using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting.Minigame;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// A character export the old build had scheduled when a hot reload activated a new build (P4-15 closure audit): the export was a
/// closure the reload dropped, and while its pending mark stays no later craft result reaches the character file.
/// </summary>
[TestFixture]
public class NwnCraftSessionWorldReloadTests
{
    private const uint Pending = 11;
    private const uint PendingDM = 12;
    private const uint Settled = 13;
    private const string PendingVariable = "CRAFT_PERSIST_PENDING";

    [Test]
    public void A_reload_makes_the_export_it_dropped_and_clears_the_mark_that_would_block_every_later_one()
    {
        var engine = new FakeNWScript();
        var locals = new LocalVariables(engine);
        using var scope = NWScriptRuntime.Override(engine);
        engine.SetReturnHandler("GetIsObjectValid", _ => true);
        engine.SetReturnHandler("GetIsPC", _ => true);
        engine.SetReturnHandler("GetIsDM", args => (uint)args[0]! == PendingDM);
        engine.SetLocalBool(Pending, PendingVariable, true);
        engine.SetLocalBool(PendingDM, PendingVariable, true);

        NwnCraftSessionWorld.ExportPendingCharactersForReload(new[] { Pending, PendingDM, Settled });

        engine.CallsTo("ExportSingleCharacter").Select(call => (uint)call.Arguments[0]!).Should().Equal(new[] { Pending },
            "a DM has no character file, and a player with nothing pending is not exported");
        locals.Has(Pending, PendingVariable).Should().BeFalse();
        locals.Has(PendingDM, PendingVariable).Should().BeFalse();
    }
}
