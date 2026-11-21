using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.ItemDefinition;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Pins what each food property subtype adds to a meal. The table was read from the switch in
/// <c>ConsumableItemDefinition.Food</c> before it was split into named steps: one subtype, one target field.
/// </summary>
public class FoodBonusMappingTests
{
    private const int Amount = 3;

    private static readonly MethodInfo AddFoodBonus = typeof(ConsumableItemDefinition)
        .GetMethod("AddFoodBonus", BindingFlags.NonPublic | BindingFlags.Static)!;

    [TestCase(FoodItemPropertySubType.HP, "HP")]
    [TestCase(FoodItemPropertySubType.FP, "FP")]
    [TestCase(FoodItemPropertySubType.STM, "STM")]
    [TestCase(FoodItemPropertySubType.HPRegen, "HPRegen")]
    [TestCase(FoodItemPropertySubType.FPRegen, "FPRegen")]
    [TestCase(FoodItemPropertySubType.STMRegen, "STMRegen")]
    [TestCase(FoodItemPropertySubType.RestRegen, "RestRegen")]
    [TestCase(FoodItemPropertySubType.XPBonus, "XPBonusPercent")]
    [TestCase(FoodItemPropertySubType.CombatReadiness, "CombatReadinessPercent")]
    [TestCase(FoodItemPropertySubType.Duration, "DURATION")]
    [TestCase(FoodItemPropertySubType.Might, "Might")]
    [TestCase(FoodItemPropertySubType.Vitality, "Vitality")]
    [TestCase(FoodItemPropertySubType.Perception, "Perception")]
    [TestCase(FoodItemPropertySubType.Willpower, "Willpower")]
    [TestCase(FoodItemPropertySubType.Agility, "Agility")]
    [TestCase(FoodItemPropertySubType.Social, "Social")]
    [TestCase(FoodItemPropertySubType.DefensePhysical, "DefensePhysical")]
    [TestCase(FoodItemPropertySubType.DefenseForce, "DefenseForce")]
    [TestCase(FoodItemPropertySubType.ResistanceFire, "ResistanceFire")]
    [TestCase(FoodItemPropertySubType.ResistancePoison, "ResistancePoison")]
    [TestCase(FoodItemPropertySubType.ResistanceElectrical, "ResistanceElectrical")]
    [TestCase(FoodItemPropertySubType.ResistanceIce, "ResistanceIce")]
    [TestCase(FoodItemPropertySubType.ResistanceMind, "ResistanceMind")]
    [TestCase(FoodItemPropertySubType.ResistanceMobility, "ResistanceMobility")]
    [TestCase(FoodItemPropertySubType.ResistanceTrauma, "ResistanceTrauma")]
    [TestCase(FoodItemPropertySubType.ResistanceDisruption, "ResistanceDisruption")]
    [TestCase(FoodItemPropertySubType.Evasion, "Evasion")]
    [TestCase(FoodItemPropertySubType.ControlSmithery, "Control[Smithery]")]
    [TestCase(FoodItemPropertySubType.CraftsmanshipSmithery, "Craftsmanship[Smithery]")]
    [TestCase(FoodItemPropertySubType.ControlEngineering, "Control[Engineering]")]
    [TestCase(FoodItemPropertySubType.CraftsmanshipEngineering, "Craftsmanship[Engineering]")]
    [TestCase(FoodItemPropertySubType.ControlFabrication, "Control[Fabrication]")]
    [TestCase(FoodItemPropertySubType.CraftsmanshipFabrication, "Craftsmanship[Fabrication]")]
    [TestCase(FoodItemPropertySubType.ControlAgriculture, "Control[Agriculture]")]
    [TestCase(FoodItemPropertySubType.CraftsmanshipAgriculture, "Craftsmanship[Agriculture]")]
    [TestCase(FoodItemPropertySubType.Accuracy, "Accuracy")]
    [TestCase(FoodItemPropertySubType.Attack, "Attack")]
    public void EachSubtypeAddsToItsOwnTargetOnly(FoodItemPropertySubType subtype, string target)
    {
        var effect = new FoodEffectData();
        var arguments = new object[] { effect, subtype, Amount, 1800f };

        AddFoodBonus.Invoke(null, arguments);

        var expected = new Dictionary<string, float>();
        var duration = (float)arguments[3];
        if (target == "DURATION")
        {
            duration.Should().Be(1800f + Amount * (60f * 5), "five minutes per duration bonus");
        }
        else
        {
            duration.Should().Be(1800f);
            expected[target] = Amount;
        }

        Snapshot(effect).Should().BeEquivalentTo(expected);
    }

    [Test]
    public void ASubtypeWithoutARowThrows()
    {
        var withRow = typeof(FoodBonusMappingTests).GetMethod(nameof(EachSubtypeAddsToItsOwnTargetOnly))!
            .GetCustomAttributes<TestCaseAttribute>()
            .Select(attribute => (FoodItemPropertySubType)attribute.Arguments[0]!)
            .ToHashSet();

        var withoutRow = Enum.GetValues<FoodItemPropertySubType>().Where(subtype => !withRow.Contains(subtype)).ToList();

        foreach (var subtype in withoutRow)
        {
            var act = () => AddFoodBonus.Invoke(null, new object[] { new FoodEffectData(), subtype, Amount, 1800f });
            act.Should().Throw<TargetInvocationException>($"{subtype} was not handled by the original switch")
                .WithInnerException<ArgumentOutOfRangeException>();
        }
    }

    [Test]
    public void AnUndefinedSubtypeThrows()
    {
        var act = () => AddFoodBonus.Invoke(null, new object[] { new FoodEffectData(), (FoodItemPropertySubType)9999, Amount, 1800f });

        act.Should().Throw<TargetInvocationException>().WithInnerException<ArgumentOutOfRangeException>();
    }

    /// <summary>The non-zero fields of the meal, named like the table: Property or Control[Skill] or Craftsmanship[Skill].</summary>
    private static Dictionary<string, float> Snapshot(FoodEffectData effect)
    {
        var result = new Dictionary<string, float>();
        foreach (var property in typeof(FoodEffectData).GetProperties().Where(p => p.PropertyType == typeof(int)))
        {
            var value = (int)property.GetValue(effect)!;
            if (value != 0)
                result[property.Name] = value;
        }

        foreach (var (skill, value) in effect.Control.Where(pair => pair.Value != 0))
            result[$"Control[{skill}]"] = value;
        foreach (var (skill, value) in effect.Craftsmanship.Where(pair => pair.Value != 0))
            result[$"Craftsmanship[{skill}]"] = value;

        return result;
    }
}
