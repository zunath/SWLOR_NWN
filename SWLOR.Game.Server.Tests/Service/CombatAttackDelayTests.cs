using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Tests.Service;

public class CombatAttackDelayTests
{
    [Test]
    public void CalculateAttackDelayMilliseconds_UsesSingleWeaponDelay()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 0, 0, 0);

        delay.Should().Be(3500);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(1750);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_DualWieldCountsDefaultDelayOnce()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 0, 0);

        delay.Should().Be(5250);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(3500);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_AppliesOffhandReductionBeforeCombiningDualWieldDelay()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 0, 30);

        delay.Should().Be(4200);
        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(2450);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_SubtractsDefaultDelayFromHigherAttackerDelay()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 2500;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(2500);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_ClampsPostBaselineDelayToDefaultMinimum()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 1250;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

        effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CalculateAttackDelayMilliseconds_FastestWeaponDelayCanBenefitFromHaste()
    {
        var unmodifiedDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 0, 0);
        var hastenOneDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 15, 0);
        var hastenTwoDelay = Combat.CalculateAttackDelayMilliseconds(290, 0, 25, 0);

        Combat.CalculateEffectiveAttackDelay(unmodifiedDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenOneDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeGreaterThan(Combat.BaseAttackDelayMilliseconds);
        Combat.CalculateEffectiveAttackDelay(hastenTwoDelay).Should().BeLessThan(Combat.CalculateEffectiveAttackDelay(hastenOneDelay));
    }

    [Test]
    public void CalculateEffectiveAttackDelay_ClampsReducedDualWieldDelayToDefaultMinimum()
    {
        var delay = Combat.CalculateAttackDelayMilliseconds(210, 210, 45, 30);

        Combat.CalculateEffectiveAttackDelay(delay).Should().Be(Combat.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultDelayWhenAttackerDelayIsSameOrLower()
    {
        var attackerDelays = new[]
        {
            0,
            Combat.BaseAttackDelayMilliseconds - 1,
            Combat.BaseAttackDelayMilliseconds
        };

        foreach (var attackerDelay in attackerDelays)
        {
            var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay);

            effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
        }
    }

    [Test]
    public void CalculateEffectiveAttackDelay_UsesDefaultMinimumWhenNoDelayAttackIsQueued()
    {
        var attackerDelay = Combat.BaseAttackDelayMilliseconds + 2000;

        var effectiveDelay = Combat.CalculateEffectiveAttackDelay(attackerDelay, true);

        effectiveDelay.Should().Be(Combat.BaseAttackDelayMilliseconds);
    }

    [Test]
    public void CanConsumeNextAbilityNoDelay_RequiresHostileAbility()
    {
        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
            {
                IsHostileAbility = true
            })
            .Should()
            .BeTrue();

        Combat.CanConsumeNextAbilityNoDelay(new AbilityDetail
            {
                IsHostileAbility = false
            })
            .Should()
            .BeFalse();
    }

    [Test]
    public void WeaponDelayMigration_CoversLivePlayerInventoryAndSerializedItems()
    {
        var root = FindRepositoryRoot();
        var playerMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "PlayerMigration",
            "_14_MigrateResistanceItemProperties.cs"));
        var serverMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "ServerMigration",
            "_31_MigrateResistanceItemProperties.cs"));
        var weaponDelayMigrationSource = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "MigrationDefinition",
            "SerializedItemWeaponDamageTypeMigration.cs"));

        playerMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateObject(player);");
        serverMigrationSource.Should().Contain("SerializedItemWeaponDamageTypeMigration.MigrateSerializedObject");
        weaponDelayMigrationSource.Should().Contain("ItemPropertyType.Delay");
        weaponDelayMigrationSource.Should().Contain("WeaponDelay.GetWeaponDelay(baseItem)");
        weaponDelayMigrationSource.Should().Contain("[\"t_knife\"] = 32");
        weaponDelayMigrationSource.Should().Contain("[\"t_shuriken\"] = 32");
        weaponDelayMigrationSource.Should().Contain("GetHasInventory(obj)");
        weaponDelayMigrationSource.Should().Contain("GetItemInSlot((InventorySlot)index, creature)");
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        directory.Should().NotBeNull("the repository root should be discoverable from the test directory");
        return directory!;
    }
}
