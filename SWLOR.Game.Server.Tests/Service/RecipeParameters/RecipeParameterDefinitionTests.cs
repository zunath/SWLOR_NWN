using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterDefinitionTests
{
    [Test]
    public void Integer_FormatsWholeNumbersAndSnapsToTheBounds()
    {
        var definition = RecipeParameterDefinition.Integer("center", "Center", 20, 80, 6);

        Assert.That(definition.FormatLiteral(49.6), Is.EqualTo("50"));
        Assert.That(definition.FormatDisplay(-0.0), Is.EqualTo("0"));
        Assert.That(definition.Snap(95), Is.EqualTo(80));
        Assert.That(definition.Snap(3), Is.EqualTo(20));
        Assert.That(definition.IsOnGrid(45), Is.True);
        Assert.That(definition.IsOnGrid(45.5), Is.False);
        Assert.That(definition.IsInBounds(81), Is.False);
    }

    [Test]
    public void Decimal_FormatsFixedDigitsWithTheInvariantCulture()
    {
        var definition = RecipeParameterDefinition.Decimal("cooling", "Cooling", 0.5, 3.0, 2);

        Assert.That(definition.FormatLiteral(1), Is.EqualTo("1.00"));
        Assert.That(definition.FormatLiteral(0.125), Is.EqualTo("0.13"));
        Assert.That(definition.FormatDisplay(2.5), Is.EqualTo("2.50"));
        Assert.That(definition.IsOnGrid(0.75), Is.True);
        Assert.That(definition.IsOnGrid(0.755), Is.False);
    }

    [Test]
    public void Enumeration_WritesTheMemberNameAndRejectsUndefinedValues()
    {
        var definition = RecipeParameterDefinition.Enumeration<TestForgeStyle>("style", "Style");

        Assert.That(definition.FormatLiteral(3), Is.EqualTo("TestForgeStyle.Hot"));
        Assert.That(definition.FormatDisplay(1), Is.EqualTo("Cold"));
        Assert.That(definition.Min, Is.EqualTo(1));
        Assert.That(definition.Max, Is.EqualTo(3));
        Assert.That(definition.IsOnGrid(2), Is.True);
        Assert.That(definition.IsOnGrid(0), Is.False);
    }

    [Test]
    public void Vary_MovesInWholeStepsInsideTheVariationAndTheBounds()
    {
        var definition = RecipeParameterDefinition.Integer("center", "Center", 20, 80, 6);

        for (var index = 0; index < 1000; index++)
        {
            var varied = definition.Vary(50, index / 1000.0);
            Assert.That(varied, Is.InRange(44, 56));
            Assert.That(varied, Is.EqualTo(Math.Round(varied)));
        }

        Assert.That(definition.Vary(50, 0.0), Is.EqualTo(44));
        Assert.That(definition.Vary(50, 0.999999), Is.EqualTo(56));
        Assert.That(definition.Vary(78, 0.999999), Is.EqualTo(80));
    }

    [TestCase(0.19, 0.1)]
    [TestCase(0.3, 0.3)]
    [TestCase(0.05, 0.0)]
    public void Vary_NeverMovesFartherThanTheDeclaredVariation(double variation, double farthest)
    {
        var definition = RecipeParameterDefinition.Decimal("cooling", "Cooling", 0.0, 3.0, 1, variation);
        var reached = 0.0;

        for (var index = 0; index < 1000; index++)
        {
            reached = Math.Max(reached, Math.Abs(definition.Vary(1.5, index / 1000.0) - 1.5));
        }

        Assert.That(reached, Is.EqualTo(farthest).Within(1e-9));
    }

    [Test]
    public void Header_CollapsesRunsOfWhitespace()
    {
        var definition = RecipeParameterDefinition.Integer("center", "  Forge   Heat\nCenter ", 0, 10);

        Assert.That(definition.Header, Is.EqualTo("Forge Heat Center"));
    }

    [Test]
    public void Vary_WithNoVariationKeepsTheBaseValue()
    {
        var definition = RecipeParameterDefinition.Integer("center", "Center", 20, 80);

        Assert.That(definition.Vary(50, 0.7), Is.EqualTo(50));
    }

    [TestCase("Center")]
    [TestCase("center value")]
    [TestCase("")]
    public void Construction_RejectsNamesThatAreNotCamelCaseIdentifiers(string name)
    {
        Assert.Throws<ArgumentException>(() => RecipeParameterDefinition.Integer(name, "Header", 0, 10));
    }

    [Test]
    public void Construction_RejectsBoundsThatAreInvertedOrOffTheGrid()
    {
        Assert.Throws<ArgumentException>(() => RecipeParameterDefinition.Integer("center", "Center", 10, 5));
        Assert.Throws<ArgumentException>(() => RecipeParameterDefinition.Decimal("cooling", "Cooling", 0.55, 3.0, 1));
    }
}
