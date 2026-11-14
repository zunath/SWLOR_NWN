using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

[TestFixture]
public class AbilityTargetingCacheServiceTests
{
    private static AbilityTargetingDetail Detail() =>
        new(default, AbilityTargetingShapeType.None, 0f, 0f, default, null!);

    [Test]
    public void StartsEmpty()
    {
        new AbilityTargetingCacheService().GetAll().Should().BeEmpty();
    }

    [Test]
    public void Set_AddsAndReplacesByFeat()
    {
        var service = new AbilityTargetingCacheService();
        var first = Detail();
        var second = Detail();

        service.Set(FeatType.Invalid, first);
        service.GetAll().Should().ContainSingle().Which.Value.Should().BeSameAs(first);

        service.Set(FeatType.Invalid, second);
        service.GetAll().Should().ContainSingle().Which.Value.Should().BeSameAs(second);
    }

    [Test]
    public void Clear_EmptiesTheCacheAndTheReadReflectsLaterChanges()
    {
        var service = new AbilityTargetingCacheService();
        var view = service.GetAll();
        service.Set(FeatType.Invalid, Detail());

        view.Should().HaveCount(1, "the read is a live view, as the static dictionary was");

        service.Clear();

        view.Should().BeEmpty();
        service.GetAll().Should().BeSameAs(view);
    }
}
