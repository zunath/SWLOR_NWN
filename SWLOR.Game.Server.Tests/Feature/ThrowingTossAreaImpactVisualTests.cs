using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition.Throwing;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Feature;

public class ThrowingTossAreaImpactVisualTests
{
    [Test]
    public void ExplosiveToss_EveryRankBurstsAtTheImpactArea()
    {
        var abilities = new ExplosiveTossAbilityDefinition().BuildAbilities();

        foreach (var feat in new[] { FeatType.ExplosiveToss1, FeatType.ExplosiveToss2, FeatType.ExplosiveToss3, FeatType.ExplosiveToss4 })
        {
            abilities[feat].AreaImpactVisualEffect.Should().Be(VisualEffect.Fnf_Fireball,
                "the throw animation is skipped while moving, so the blast must still show where it lands");
        }
    }

    [Test]
    public void ConcussiveToss_EveryRankBurstsAtTheImpactArea()
    {
        var abilities = new ConcussiveTossAbilityDefinition().BuildAbilities();

        foreach (var feat in new[] { FeatType.ConcussiveToss1, FeatType.ConcussiveToss2 })
        {
            abilities[feat].AreaImpactVisualEffect.Should().Be(VisualEffect.Vfx_Fnf_Sound_Burst,
                "the throw animation is skipped while moving, so the blast must still show where it lands");
        }
    }
}
