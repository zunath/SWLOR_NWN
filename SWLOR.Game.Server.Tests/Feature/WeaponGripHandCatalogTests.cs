using System;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponGripHandCatalogTests
{
    private static WeaponGripHandCatalog Catalog() => new(new[]
    {
        ("pmd0_handr257", "pmd0_handr30000"),
        ("pmd0_handl257", "pmd0_handl30000")
    });

    [TestCase(BaseItem.Pistol)]
    [TestCase(BaseItem.Sling)]
    [TestCase(BaseItem.LegacyPistol)]
    [TestCase(BaseItem.CompanionPistol)]
    public void BothPistolHandsUseTheirOwnGraspPose(BaseItem weapon)
    {
        foreach (var side in new[] { "r", "l" })
            Catalog().Resolve($"pmd0_hand{side}257", weapon, side).Should().Be($"pmd0_hand{side}30000");
    }

    [Test]
    public void RifleTriggerHandUsesGraspWhileSupportPalmKeepsItsAuthoredPose()
    {
        var catalog = Catalog();
        catalog.Resolve("pmd0_handr257", BaseItem.Rifle, "r").Should().Be("pmd0_handr30000");
        catalog.Resolve("pmd0_handl257", BaseItem.Rifle, "l").Should().Be("pmd0_handl257");
    }

    [Test]
    public void UnequippedAndOtherWeaponRolesResolveBackToTheAuthoredStyle()
    {
        var catalog = Catalog();
        foreach (var weapon in new[] { (BaseItem)(-1), BaseItem.Dagger, BaseItem.Rifle })
            catalog.Resolve("pmd0_handl30000", weapon, "l").Should().Be("pmd0_handl257");
        catalog.Authored("PMD0_HANDL30000").Should().Be("pmd0_handl257");
        catalog.AuthoredStyle("pmd0_handl", 30000).Should().Be(257);
        catalog.Resolve("pmd0_handl001", BaseItem.Pistol, "l").Should().Be("pmd0_handl001");
    }

    [TestCase("pmd0_handl257", "pmd0_handr30000")]
    [TestCase("pmd0_handl257", "pfh0_handl30000")]
    [TestCase("pmd2_handl257", "pmd0_handl30000")]
    [TestCase("pmd0_handl257", "pmd0_handl999")]
    [TestCase("pmd0_handl257", "pmd0_handl65536")]
    [TestCase("pmd0_handl000", "pmd0_handl30000")]
    public void RowsCannotCrossBodiesSidesOrTheNativeStyleRange(string source, string render)
    {
        Action load = () => _ = new WeaponGripHandCatalog(new[] { (source, render) });
        load.Should().Throw<ArgumentException>();
    }

    [Test]
    public void DuplicateSourcesAndAmbiguousRenderAliasesAreRejected()
    {
        Action duplicateSource = () => _ = new WeaponGripHandCatalog(new[]
        {
            ("pmd0_handl257", "pmd0_handl30000"), ("PMD0_HANDL257", "pmd0_handl30001")
        });
        Action duplicateRender = () => _ = new WeaponGripHandCatalog(new[]
        {
            ("pmd0_handl257", "pmd0_handl30000"), ("pmd0_handl258", "PMD0_HANDL30000")
        });
        duplicateSource.Should().Throw<ArgumentException>();
        duplicateRender.Should().Throw<ArgumentException>();
    }

    [Test]
    public void NaturalHandStylesCanUseIndependentGraspAndSupportPoses()
    {
        var catalog = new WeaponGripHandCatalog(new[] { ("pmh0_handl002", "pmh0_handl30002") });
        catalog.Resolve("pmh0_handl002", BaseItem.CompanionPistol, "l").Should().Be("pmh0_handl30002");
        catalog.Resolve("pmh0_handl30002", BaseItem.Rifle, "l").Should().Be("pmh0_handl002");
        catalog.AuthoredStyle("pmh0_handl", 30002).Should().Be(2);
    }

    [TestCase(BaseItem.Pistol)]
    [TestCase(BaseItem.Sling)]
    [TestCase(BaseItem.LegacyPistol)]
    [TestCase(BaseItem.CompanionPistol)]
    public void ModelSpecificGraspSwitchesWithoutLosingTheAuthoredStyle(BaseItem weapon)
    {
        var catalog = new WeaponGripHandCatalog(new[]
        {
            new WeaponGripHandRow("pmz0_handr208", "pmz0_handr32308"),
            new WeaponGripHandRow("pmz0_handr208", "pmz0_handr32309", BaseItem.Pistol, 122),
            new WeaponGripHandRow("PMZ0_HANDR208", "PMZ0_HANDR32309", BaseItem.Sling, 125)
        });
        catalog.Resolve("pmz0_handr208", weapon, "r", 301).Should().Be("pmz0_handr32308");
        catalog.Resolve("pmz0_handr32308", weapon, "r", 122).Should().Be("pmz0_handr32309");
        catalog.Resolve("pmz0_handr32309", weapon, "r", 125).Should().Be("PMZ0_HANDR32309");
        catalog.Resolve("PMZ0_HANDR32309", weapon, "r", 301).Should().Be("pmz0_handr32308");
        catalog.AuthoredStyle("pmz0_handr", 32309).Should().Be(208);
        catalog.Resolve("pmz0_handr32309", (BaseItem)(-1), "r").Should().Be("pmz0_handr208");
    }

    [Test]
    public void ExplicitRifleSupportHasClassAndModelPrecedenceAndRestoresOnUnequip()
    {
        var catalog = new WeaponGripHandCatalog(new[]
        {
            new WeaponGripHandRow("pfo0_handl003", "pfo0_handl30003"),
            new WeaponGripHandRow("pfo0_handl003", "pfo0_handl31703", BaseItem.Rifle),
            new WeaponGripHandRow("pfo0_handl003", "pfo0_handl31704", BaseItem.Rifle, 101)
        });
        catalog.ResolveStyle("pfo0_handl", 3, BaseItem.Pistol, "l", 101).Should().Be(30003);
        catalog.ResolveStyle("pfo0_handl", 30003, BaseItem.Rifle, "l", 102).Should().Be(31703);
        catalog.ResolveStyle("pfo0_handl", 31703, BaseItem.Rifle, "l", 101).Should().Be(31704);
        catalog.ResolveStyle("pfo0_handl", 31704, (BaseItem)(-1), "l").Should().Be(3);
    }

    [Test]
    public void CanonicalDuplicateSelectionsAndAmbiguousModelAliasesAreRejected()
    {
        Action duplicate = () => _ = new WeaponGripHandCatalog(new[]
        {
            new WeaponGripHandRow("pmz0_handr208", "pmz0_handr32309", BaseItem.Pistol, 122),
            new WeaponGripHandRow("PMZ0_HANDR208", "pmz0_handr32308", BaseItem.Sling, 122)
        });
        Action ambiguous = () => _ = new WeaponGripHandCatalog(new[]
        {
            new WeaponGripHandRow("pmz0_handr208", "pmz0_handr32309", BaseItem.Pistol, 122),
            new WeaponGripHandRow("pmz0_handr214", "PMZ0_HANDR32309", BaseItem.Pistol, 125)
        });
        duplicate.Should().Throw<ArgumentException>();
        ambiguous.Should().Throw<ArgumentException>();
    }
}
