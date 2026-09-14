using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class RestStatusEffectTests
{
    [Test]
    public void RestStatusEffect_UsesSlowerRecoveryTick()
    {
        var effect = new RestStatusEffect();

        effect.Frequency.Should().Be(6f);
    }
}
