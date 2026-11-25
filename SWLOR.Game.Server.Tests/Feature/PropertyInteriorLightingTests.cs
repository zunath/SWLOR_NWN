using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.PropertyLayoutDefinition;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Domain.Foundation;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Player property interiors are instanced from these templates. A template with the
/// day/night cycle on switches to its moon lighting at night, which leaves homes dark.
/// </summary>
public class PropertyInteriorLightingTests
{
    private const int InteriorFlag = 0x1;

    private static IEnumerable<TestCaseData> InteriorTemplates()
    {
        // The layout list interface is internal, so find its implementations by name.
        var assembly = typeof(HouseLayoutDefinition).Assembly;
        var definitionInterface = assembly.GetType("SWLOR.Game.Server.Domain.World.IPropertyLayoutListDefinition", true)!;
        return assembly.GetTypes()
            .Where(type => definitionInterface.IsAssignableFrom(type) &&
                           type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => ((Dictionary<PropertyLayoutType, PropertyLayout>)
                definitionInterface.GetMethod("Build")!.Invoke(Activator.CreateInstance(type), null)!).Values)
            .Select(layout => layout.AreaInstanceResref)
            .Where(resref => !string.IsNullOrWhiteSpace(resref))
            .Distinct()
            .OrderBy(resref => resref)
            .Select(resref => new TestCaseData(resref).SetName($"PropertyInterior_{resref}"));
    }

    [Test]
    public void EveryPropertyType_IsCovered()
    {
        InteriorTemplates().Select(data => (string)data.Arguments[0]!)
            .Should().Contain(new[] { "house_int_5", "bank", "city_hall", "medical_center", "playerap_m_unf", "ship_fight_v" });
    }

    [TestCaseSource(nameof(InteriorTemplates))]
    public void Template_IsInteriorWithFixedLighting(string resref)
    {
        using var area = Load(resref);
        var root = area.RootElement;

        (Value(root, "Flags") & InteriorFlag).Should().Be(InteriorFlag, $"{resref} must be flagged interior");
        Value(root, "DayNightCycle").Should().Be(0, $"{resref} must not follow the outdoor day/night cycle");
    }

    [TestCase("house_int_1")]
    [TestCase("house_int_5")]
    [TestCase("house_int_12")]
    [TestCase("bank")]
    public void FormerlyCyclingTemplates_KeepTheirDaytimeLighting(string resref)
    {
        using var area = Load(resref);
        var root = area.RootElement;

        // Always day, so the authored sun colors stay lit all the time.
        Value(root, "IsNight").Should().Be(0);
        Value(root, "SunAmbientColor").Should().Be(6566450);
        Value(root, "SunDiffuseColor").Should().Be(16777215);
    }

    private static long Value(JsonElement root, string field) =>
        root.GetProperty(field).GetProperty("value").GetInt64();

    private static JsonDocument Load(string resref)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        var root = directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(root.FullName, "Module", "are", resref + ".are.json")));
    }
}
