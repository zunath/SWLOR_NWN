using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;

namespace SWLOR.Game.Server.Tests.Feature;

public class RestStatusEffectTests
{
    [Test]
    public void RestStatusEffect_UsesSlowerRecoveryTick()
    {
        var effect = new RestStatusEffect();

        effect.Frequency.Should().Be(6f);
    }

    [Test]
    public void A_reload_restarts_the_movement_check_whose_closure_it_dropped()
    {
        const uint resting = 101;
        var engine = new FakeNWScript();
        engine.SetReturnHandler("GetIsObjectValid", _ => false);
        using var scope = NWScriptRuntime.Override(engine);

        new RestStatusEffect().ResumeAfterReload(resting);

        var check = engine.CallsTo("DelayCommand").Should().ContainSingle().Subject;
        check.Arguments[0].Should().Be(0.5f, "the check runs every half second, as it did before the reload");
        ((Action)check.Arguments[1]!)();
        engine.CallsTo("DelayCommand").Should().ContainSingle("the check stops for a creature that is gone");
    }
}
