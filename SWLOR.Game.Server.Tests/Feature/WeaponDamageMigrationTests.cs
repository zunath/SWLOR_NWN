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
        var method = typeof(_22_CombatSystemReplacement).Assembly
            .GetType("SWLOR.Game.Server.Feature.MigrationDefinition.SerializedItemWeaponDamageTypeMigration")!
            .GetMethod("ShouldMigrate", BindingFlags.NonPublic | BindingFlags.Static)!;
        var damage = new List<(ItemProperty Property, int SubType, int Value)> { (null!, subtype, 5) };
        var types = new List<(ItemProperty Property, int SubType)>();

        method.Invoke(null, new object[] { BaseItem.Longsword, damage, types, CombatDamageType.Physical })
            .Should().Be(false, "NWN persists the absent subtype as an unsigned 16-bit sentinel");
    }
}
