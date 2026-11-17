using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Domain.Foundation;

[TestFixture]
public class DamageTableDefaultsTests
{
    [Test]
    public void DefenseDefaults_HoldEveryDefenseTypeInEnumOrder()
    {
        var defenses = DamageTableDefaults.CreateDefaultDefenseValues();
        defenses.Keys.Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
        defenses.Values.Should().OnlyContain(value => value == 0);
        DamageTableDefaults.CreateDefaultDefenseValues(7).Values.Should().OnlyContain(value => value == 7);
        DamageTableDefaults.CreateDefaultDefenseValues().Should().NotBeSameAs(defenses);
    }

    [Test]
    public void DefenseDamageTypes_AreTheCatalogsListInEnumOrder()
    {
        var catalog = new CombatDamageCatalogService(null!, null!);
        catalog.LoadDamageTypes();

        DamageTableDefaults.GetDefenseDamageTypes().Should().Equal(CombatDamageType.Physical, CombatDamageType.Force);
        DamageTableDefaults.GetDefenseDamageTypes().Should().Equal(catalog.GetDefenseDamageTypes());
    }

    [Test]
    public void ResistanceDefaults_HoldEveryTypeButInvalid()
    {
        var resistances = DamageTableDefaults.CreateDefaultResistanceValues(4);
        resistances.Keys.Should().Equal(DamageTableDefaults.EnumerateResistanceTypes());
        resistances.Keys.Should().NotContain(ResistanceType.Invalid);
        resistances.Values.Should().OnlyContain(value => value == 4);
    }

    [Test]
    public void TheForwardersMasterStillCallReturnTheSameTables()
    {
        CombatRules.CreateDefaultDefenseValues(2).Should().Equal(DamageTableDefaults.CreateDefaultDefenseValues(2));
        ResistanceRules.CreateDefaultResistanceValues(3).Should().Equal(DamageTableDefaults.CreateDefaultResistanceValues(3));
        ResistanceRules.EnumerateResistanceTypes().Should().Equal(DamageTableDefaults.EnumerateResistanceTypes());
    }
}
