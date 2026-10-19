using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class WeaponDamageMigrationTests
{
    /// <summary>
    /// Guards idempotence for weapons already using the canonical untyped damage representation.
    /// </summary>
    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(ushort.MaxValue)]
    public void CanonicalUntypedDamageDoesNotNeedAnotherConversion(int subtype)
    {
        var method = typeof(SWLOR.Game.Server.Service.Migration).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.SerializedItemWeaponDamageTypeMigration")!
            .GetMethod("ShouldMigrate", BindingFlags.NonPublic | BindingFlags.Static)!;
        var damage = new List<(ItemProperty Property, int SubType, int Value)> { (null!, subtype, 5) };
        var types = new List<(ItemProperty Property, int SubType)>();

        method.Invoke(null, new object[] { BaseItem.Longsword, damage, types, CombatDamageType.Physical })
            .Should().Be(false, "NWN persists the absent subtype as an unsigned 16-bit sentinel");
    }

    /// <summary>
    /// iprp_c_dmgtype has no Force row, so legacy Force damage must resolve to Physical instead of
    /// producing a weapon damage type property that cannot be constructed.
    /// </summary>
    [Test]
    public void LegacyForceDamageResolvesToPhysical()
    {
        var migration = typeof(SWLOR.Game.Server.Service.Migration).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.SerializedItemWeaponDamageTypeMigration")!;
        var resolve = migration.GetMethod("ResolveDamageType", BindingFlags.NonPublic | BindingFlags.Static)!;
        var shouldMigrate = migration.GetMethod("ShouldMigrate", BindingFlags.NonPublic | BindingFlags.Static)!;
        var noDamage = new List<(ItemProperty Property, int SubType, int Value)>();
        var noTypes = new List<(ItemProperty Property, int SubType)>();
        var forceDamage = new List<(ItemProperty Property, int SubType, int Value)> { (null!, (int)CombatDamageType.Force, 6) };
        var forceType = new List<(ItemProperty Property, int SubType)> { (null!, (int)CombatDamageType.Force) };
        var forceAndFire = new List<(ItemProperty Property, int SubType)> { (null!, (int)CombatDamageType.Force), (null!, (int)CombatDamageType.Fire) };

        resolve.Invoke(null, new object[] { forceDamage, noTypes }).Should().Be(CombatDamageType.Physical);
        resolve.Invoke(null, new object[] { noDamage, forceType }).Should().Be(CombatDamageType.Physical);
        resolve.Invoke(null, new object[] { noDamage, forceAndFire }).Should().Be(CombatDamageType.Fire);

        var canonicalDamage = new List<(ItemProperty Property, int SubType, int Value)> { (null!, -1, 5) };
        shouldMigrate.Invoke(null, new object[] { BaseItem.Longsword, canonicalDamage, forceType, CombatDamageType.Physical })
            .Should().Be(true, "a leftover Force damage type property no longer retypes the weapon and is removed");
    }
}
