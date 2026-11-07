using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// <c>CombatDamageTypeExtensions</c> reads the details of a damage type from a frozen table of <see cref="CombatDamageDetails"/>
/// built from the <see cref="CombatDamageAttribute"/> on each enum value. Every value must give exactly what its attribute
/// says, and the table is read by key only.
/// </summary>
[TestFixture]
public class CombatDamageDetailsTests
{
    private static IEnumerable<CombatDamageType> Types() => Enum.GetValues<CombatDamageType>();

    private static CombatDamageAttribute Attribute(CombatDamageType type) =>
        typeof(CombatDamageType).GetField(type.ToString())!.GetCustomAttribute<CombatDamageAttribute>()!;

    [TestCaseSource(nameof(Types))]
    public void EveryTypeGivesExactlyWhatItsAttributeSays(CombatDamageType type)
    {
        var attribute = Attribute(type);
        var details = type.GetDetails();

        details.Should().Be(CombatDamageDetails.From(attribute));
        details.Category.Should().Be(attribute.Category);
        details.DefenseDamageType.Should().Be(attribute.DefenseDamageType);
        details.DefenseAbility.Should().Be(attribute.DefenseAbility);
        details.ElementalResistanceType.Should().Be(attribute.ElementalResistanceType);
        details.SourceResistanceType.Should().Be(attribute.SourceResistanceType);
        details.NWScriptDamageType.Should().Be(attribute.NWScriptDamageType);
        details.NativeDamageType.Should().Be(attribute.NativeDamageType);
        type.GetNativeDamageType().Should().Be(attribute.NativeDamageType);
        type.GetNWScriptDamageType().Should().Be(attribute.NWScriptDamageType);
        type.GetDetails().Should().BeSameAs(details, "the table is built once");
    }

    [Test]
    public void AValueWithoutAnAttributeIsRejectedAsBefore()
    {
        var unknown = (CombatDamageType)999;

        var read = () => unknown.GetDetails();

        read.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("type")
            .WithMessage("No CombatDamageAttribute is registered for combat damage type 999.*");
    }

    [Test]
    public void TheCategoryQuestionsFollowTheDetails()
    {
        CombatDamageType.Physical.IsPhysicalDamageType().Should().BeTrue();
        CombatDamageType.Fire.IsElementalDamageType().Should().BeTrue();
        CombatDamageType.Force.IsDefenseDamageType().Should().BeTrue();
        CombatDamageType.Thermal.IsCharacterDamageType().Should().BeFalse();
        CombatDamageType.Ice.TryGetElementalResistanceType(out var resistance).Should().BeTrue();
        resistance.Should().Be(ResistanceType.Ice);
        CombatDamageType.Sonic.TryGetSourceResistanceType(out _).Should().BeFalse();
        CombatDamageType.Fire.GetDefenseDamageType().Should().Be(CombatDamageType.Physical);
        CombatDamageType.Force.GetDefenseAbilityType().Should().Be(SWLOR.NWN.API.NWScript.Enum.AbilityType.Willpower);
    }
}
