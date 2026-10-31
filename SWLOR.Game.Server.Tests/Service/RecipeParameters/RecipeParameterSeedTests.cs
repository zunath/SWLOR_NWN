using NUnit.Framework;
using SWLOR.Game.Server.Service.CraftService.RecipeParameters;

namespace SWLOR.Game.Server.Tests.Service.RecipeParameters;

public class RecipeParameterSeedTests
{
    [TestCase("", 0xcbf29ce484222325UL)]
    [TestCase("a", 0xaf63dc4c8601ec8cUL)]
    [TestCase("foobar", 0x85944171f73967e8UL)]
    public void Hash_IsFnv1a64(string text, ulong expected)
    {
        Assert.That(RecipeParameterSeed.Hash(text), Is.EqualTo(expected));
    }

    [Test]
    public void Unit_IsPinnedSoGeneratedValuesNeverChangeBetweenRunsOrMachines()
    {
        Assert.That(RecipeParameterSeed.Unit("TestForge", "BattlemasterBreastplate", "center", 0),
            Is.EqualTo(0.2182971813512934).Within(1e-15));
        Assert.That(RecipeParameterSeed.Unit("TestForge", "BattlemasterBreastplate", "center", 1),
            Is.EqualTo(0.5825093647981229).Within(1e-15));
    }

    [Test]
    public void Unit_StaysInsideZeroToOne()
    {
        for (var index = 0; index < 5000; index++)
        {
            var value = RecipeParameterSeed.Unit("TestForge", "Recipe" + index, "center", index % 7);
            Assert.That(value, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
        }
    }

    [Test]
    public void Unit_SpreadsAcrossTheRange()
    {
        var buckets = new int[10];
        for (var index = 0; index < 10000; index++)
        {
            buckets[(int)(RecipeParameterSeed.Unit("TestForge", "Recipe" + index, "center", 0) * 10)]++;
        }

        Assert.That(buckets, Has.All.InRange(800, 1200));
    }

    [Test]
    public void Unit_DependsOnEveryInput()
    {
        var baseline = RecipeParameterSeed.Unit("TestForge", "RecipeA", "center", 0);

        Assert.That(RecipeParameterSeed.Unit("TestForge", "RecipeA", "center", 0), Is.EqualTo(baseline));
        Assert.That(RecipeParameterSeed.Unit("OtherForge", "RecipeA", "center", 0), Is.Not.EqualTo(baseline));
        Assert.That(RecipeParameterSeed.Unit("TestForge", "RecipeB", "center", 0), Is.Not.EqualTo(baseline));
        Assert.That(RecipeParameterSeed.Unit("TestForge", "RecipeA", "width", 0), Is.Not.EqualTo(baseline));
        Assert.That(RecipeParameterSeed.Unit("TestForge", "RecipeA", "center", 1), Is.Not.EqualTo(baseline));
    }
}
