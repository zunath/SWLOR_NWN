using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class AbilityBuilderTests
{
    [Test]
    public void SkillType_DoesNotApplyImpactAnimationDefault()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .SkillType(SkillType.Pistol)
            .Build();

        abilities[FeatType.Invalid].ImpactAnimationType.Should().Be(Animation.Invalid);
    }

    [Test]
    public void SkillType_PreservesExplicitImpactAnimation()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .UsesImpactAnimation(Animation.QuickDraw)
            .SkillType(SkillType.Pistol)
            .Build();

        abilities[FeatType.Invalid].ImpactAnimationType.Should().Be(Animation.QuickDraw);
    }

    [Test]
    public void SkillType_LeavesImpactAnimationUnset()
    {
        var abilities = new AbilityBuilder()
            .Create(FeatType.Invalid, PerkType.Invalid)
            .SkillType(SkillType.Lightsaber)
            .Build();

        abilities[FeatType.Invalid].ImpactAnimationType.Should().Be(Animation.Invalid);
    }
}
