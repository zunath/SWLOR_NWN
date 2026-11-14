using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Service;

public class WeaponAppearanceIdentityCacheTests
{
    private static WeaponAppearanceKey Appearance(int middle) => new(7, 101, middle, 101);

    [Test]
    public void RepeatedModelSwitchesReuseOnlyTheTwoImmutableMeshesAndKeepBothInboundReferences()
    {
        var cache = new WeaponAppearanceIdentityCache();
        var allocations = 0u;
        for (var index = 0; index < 100; index++)
        {
            var identity = cache.Select(10, Appearance(index % 2 == 0 ? 319 : 420), () => 100 + ++allocations, out var changed);
            identity.Should().Be(index % 2 == 0 ? 101u : 102u);
            changed.Should().BeTrue();
            cache.Read(101).Should().Be(10);
            if (index > 0) cache.Read(102).Should().Be(10);
            cache.Write(10).Should().Be(identity);
        }
        allocations.Should().Be(2);
        cache.Identities(10).Should().BeEquivalentTo(new[] { 101u, 102u });
    }

    [Test]
    public void ReapplyingTheCurrentAppearanceDoesNotAcquireAnotherDrawGuard()
    {
        var cache = new WeaponAppearanceIdentityCache();
        cache.Select(10, Appearance(319), () => 101, out _);
        cache.Select(10, Appearance(319), () => throw new Exception("Unexpected allocation"), out var changed).Should().Be(101);
        changed.Should().BeFalse();
    }

    [Test]
    public void DifferentItemsAndOrdinaryObjectReferencesRemainIndependent()
    {
        var cache = new WeaponAppearanceIdentityCache();
        cache.Select(10, Appearance(319), () => 101, out _);
        cache.Select(20, Appearance(319), () => 102, out _);
        cache.Write(10).Should().Be(101);
        cache.Write(20).Should().Be(102);
        cache.Read(101).Should().Be(10);
        cache.Read(102).Should().Be(20);
        cache.Write(30).Should().Be(30);
        cache.Read(30).Should().Be(30);
    }

    [Test]
    public void InvalidOrReusedReservationsCannotRedirectAnotherItemsReference()
    {
        var cache = new WeaponAppearanceIdentityCache();
        cache.Select(10, Appearance(319), () => 101, out _);
        foreach (var invalid in new[] { 0x7f000000u, 20u, 101u })
        {
            var select = () => cache.Select(20, Appearance(420), () => invalid, out _);
            select.Should().Throw<InvalidOperationException>();
        }
        cache.Write(20).Should().Be(20);
        cache.Read(101).Should().Be(10);
        cache.Items.Should().BeEquivalentTo(new[] { 10u });
    }

    [Test]
    public void CleanupRemovesEveryAppearanceWithoutDisturbingAnotherItem()
    {
        var cache = new WeaponAppearanceIdentityCache();
        cache.Select(10, Appearance(319), () => 101, out _);
        cache.Select(10, Appearance(420), () => 102, out _);
        cache.Select(20, Appearance(420), () => 103, out _);
        cache.Forget(10);
        cache.Write(10).Should().Be(10);
        cache.Read(101).Should().Be(101);
        cache.Read(102).Should().Be(102);
        cache.Identities(10).Should().BeEmpty();
        cache.Read(103).Should().Be(20);
        cache.Write(20).Should().Be(103);
    }

    [Test]
    public void AllMeshFieldsParticipateInTheCacheSelection()
    {
        var cache = new WeaponAppearanceIdentityCache();
        var index = 100u;
        foreach (var appearance in new[] { Appearance(319), new WeaponAppearanceKey(61, 101, 319, 101),
                     new WeaponAppearanceKey(7, 201, 319, 101), new WeaponAppearanceKey(7, 101, 319, 201) })
            cache.Select(10, appearance, () => ++index, out _).Should().Be(index);
        cache.Identities(10).Should().HaveCount(4);
    }
}
