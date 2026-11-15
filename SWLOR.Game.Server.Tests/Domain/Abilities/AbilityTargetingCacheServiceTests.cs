using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
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

    private static AbilityDetail Ability(string name, AbilityTargetingDetail? targeting) => new() { Name = name, Targeting = targeting };

    [Test]
    public void CacheData_KeepsOnlyTheOverridesThatUpdateTheClient_AndReplacesTheOldOnes()
    {
        var service = new AbilityTargetingCacheService();
        service.Set(FeatType.Invalid, Detail());
        var client = new AbilityTargetingDetail(Spell.Fireball, AbilityTargetingShapeType.Sphere, 5f, 0f, default, null!);
        var serverOnly = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Sphere, 5f, 0f, default, null!, updatesClientTargeting: false);

        service.CacheData(new Dictionary<FeatType, AbilityDetail>
        {
            [FeatType.Flash1] = Ability("Client", client),
            [FeatType.ForcePush1] = Ability("Server only", serverOnly),
            [FeatType.ForcePush2] = Ability("No targeting", null)
        });

        service.GetAll().Should().ContainSingle().Which.Should().Be(new KeyValuePair<FeatType, AbilityTargetingDetail>(FeatType.Flash1, client));
    }

    [Test]
    public void CacheData_RejectsTargetingWithoutASpellAnUnknownShapeOrABadSize()
    {
        var service = new AbilityTargetingCacheService();

        foreach (var bad in new[]
                 {
                     new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Sphere, 5f, 0f, default, null!),
                     new AbilityTargetingDetail(Spell.Fireball, (AbilityTargetingShapeType)99, 5f, 0f, default, null!),
                     new AbilityTargetingDetail(Spell.Fireball, AbilityTargetingShapeType.Sphere, 0f, 0f, default, null!),
                     new AbilityTargetingDetail(Spell.Fireball, AbilityTargetingShapeType.Sphere, 5f, -1f, default, null!)
                 })
        {
            var cache = () => service.CacheData(new Dictionary<FeatType, AbilityDetail> { [FeatType.Flash1] = Ability("Bad", bad) });
            cache.Should().Throw<InvalidOperationException>();
        }
    }

    [Test]
    public void ValidateSize_AcceptsAPositiveWidthAndANonNegativeHeight()
    {
        var valid = () => AbilityTargetingCacheService.ValidateSize(FeatType.Flash1, 5f, 0f);
        var infinite = () => AbilityTargetingCacheService.ValidateSize(FeatType.Flash1, float.PositiveInfinity, 0f);

        valid.Should().NotThrow();
        infinite.Should().Throw<InvalidOperationException>().WithMessage("*size X*");
    }
}
