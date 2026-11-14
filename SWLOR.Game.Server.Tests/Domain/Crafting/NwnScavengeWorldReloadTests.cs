using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Domain.Crafting;

/// <summary>
/// Scavenge sites when a hot reload has activated a new build (P4-15 closure audit): a hold's lapse and a spent site's destruction are
/// closures the reload drops, so the new build checks each held site again and destroys each spent one.
/// </summary>
[TestFixture]
public class NwnScavengeWorldReloadTests
{
    private const uint Invalid = 0x7F000000;
    private const uint Area = 900;
    private const uint HeldSite = 901;
    private const uint LapsedSite = 902;
    private const uint SpentSite = 903;
    private const uint FreshSite = 904;
    private const uint Chest = 905;
    private const uint Scavenger = 11;
    private const uint GoneScavenger = 12;
    private const uint Leftover = 990;

    [Test]
    public void A_reload_checks_held_sites_again_lapses_the_holds_of_scavengers_gone_and_destroys_spent_sites()
    {
        var engine = new FakeNWScript();
        var locals = new LocalVariables(engine);
        using var scope = NWScriptRuntime.Override(engine);
        engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! is not Invalid and not GoneScavenger);
        engine.SetReturn("GetFirstArea", Area);
        engine.SetReturn("GetNextArea", Invalid);
        engine.SetReturn("GetFirstObjectInArea", HeldSite);
        engine.SetReturnSequence("GetNextObjectInArea", LapsedSite, SpentSite, FreshSite, Chest, Invalid);
        engine.SetReturnHandler("GetEventScript", args =>
            (uint)args[0]! != Chest && (EventScript)args[1]! == EventScript.Placeable_OnClosed ? "scav_closed" : "chest_closed");
        engine.SetReturnHandler("GetFirstItemInInventory", args => (uint)args[0]! is HeldSite or LapsedSite ? Leftover : Invalid);
        foreach (var searched in new[] { HeldSite, LapsedSite, SpentSite, Chest })
            engine.SetLocalBool(searched, NwnScavengeWorld.SearchedVariable, true);
        engine.SetLocalObject(HeldSite, NwnScavengeWorld.OpeningVariable, Scavenger);
        engine.SetLocalObject(LapsedSite, NwnScavengeWorld.OpeningVariable, GoneScavenger);
        engine.SetLocalBool(LapsedSite, NwnScavengeWorld.HolderOpenedVariable, true);

        NwnScavengeWorld.SettleSitesForReload(Array.Empty<uint>());

        engine.CallsTo("AssignCommand").Should().ContainSingle("only the hold of the scavenger still online is checked again");
        locals.Object(HeldSite, NwnScavengeWorld.OpeningVariable).Should().Be(Scavenger);
        locals.Has(LapsedSite, NwnScavengeWorld.OpeningVariable).Should().BeFalse("its scavenger has gone, so what it holds is anyone's");
        locals.Has(LapsedSite, NwnScavengeWorld.HolderOpenedVariable).Should().BeFalse();
        engine.CallsTo("SetPlotFlag").Should().ContainSingle().Which.Arguments[0].Should().Be(SpentSite);
        engine.CallsTo("ApplyEffectToObject").Should().ContainSingle().Which.Arguments[2].Should().Be(SpentSite,
            "a searched site with nothing left and no hold was waiting to be destroyed; an unsearched one and a chest stay");
    }
}
