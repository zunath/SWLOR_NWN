using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.Game.Server.Service.PerformanceService;

namespace SWLOR.Game.Server.Tests.Feature;

public class MeleeWeaponCarryTests
{
    private static IEnumerable<string> InstrumentResrefs => PerformanceInstrumentCatalog.Resrefs;

    [TestCaseSource(nameof(InstrumentResrefs))]
    public void PlayableInstrumentsKeepTheirNativeCarry(string resref)
    {
        MeleeWeaponCarry.UsesLoweredCarry(AnimationWeaponGrip.Polearm, resref).Should().BeFalse();
    }

    [TestCase(AnimationWeaponGrip.TwoHanded)]
    [TestCase(AnimationWeaponGrip.Polearm)]
    public void MeleeWeaponsUseLoweredCarry(AnimationWeaponGrip grip)
    {
        MeleeWeaponCarry.UsesLoweredCarry(grip, "melee_weapon").Should().BeTrue();
    }

    [Test]
    public void OneHandedWeaponsKeepTheirNativeCarry()
    {
        MeleeWeaponCarry.UsesLoweredCarry(AnimationWeaponGrip.OneHanded, "melee_weapon").Should().BeFalse();
    }
}
