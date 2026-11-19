using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
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

    [Test]
    public void PreEquippedNpcCarryIsSynchronizedOnSpawn()
    {
        var scripts = typeof(MeleeWeaponCarry).GetMethod(nameof(MeleeWeaponCarry.OnCreatureSpawn))!
            .GetCustomAttributes(typeof(NWNEventHandler), false)
            .Cast<NWNEventHandler>()
            .Select(attribute => attribute.Script);

        scripts.Should().Contain(ScriptName.OnCreatureSpawnAfter);
        MeleeWeaponCarry.UsesLoweredCarry(AnimationWeaponGrip.TwoHanded, "npc_saberstaff").Should().BeTrue();
        MeleeWeaponCarry.UsesLoweredCarry(AnimationWeaponGrip.Polearm, "npc_polearm").Should().BeTrue();
    }

    [Test]
    public void LoginResolvesCarryBeforeKatarAndPistolMappings()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null &&
               (!File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) ||
                !Directory.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server"))))
            directory = directory.Parent;

        directory.Should().NotBeNull("the login wiring source should be available for review");
        var source = File.ReadAllText(Path.Combine(
            directory!.FullName, "SWLOR.Game.Server", "Feature", "SavedAnimationMappings.cs"));

        var clear = source.IndexOf("ReplaceObjectAnimation(player, source);", StringComparison.Ordinal);
        var carry = source.IndexOf("MeleeWeaponCarry.Synchronize(player);", StringComparison.Ordinal);
        var katar = source.IndexOf("KatarAnimationRemap.SynchronizeOnLogin(player);", StringComparison.Ordinal);
        var pistol = source.IndexOf("PistolAnimationRemap.SynchronizeOnLogin(player);", StringComparison.Ordinal);

        clear.Should().BeGreaterThanOrEqualTo(0);
        carry.Should().BeGreaterThan(clear);
        katar.Should().BeGreaterThan(carry, "Power Glove movement mappings must be applied after carry cleanup");
        pistol.Should().BeGreaterThan(katar);
    }
}
