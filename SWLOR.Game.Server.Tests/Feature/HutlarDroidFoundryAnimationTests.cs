using System.Text.Json;
using FluentAssertions;
using Nwn.Formats.NativeModels;
using NUnit.Framework;
using SWLOR.Game.Server.Feature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.Formats.TwoDA;

namespace SWLOR.Game.Server.Tests.Feature;

public class HutlarDroidFoundryAnimationTests
{
    [TestCase("fdy_sentry")]
    [TestCase("fdy_welder")]
    [TestCase("fdy_linedef")]
    [TestCase("fdy_reclaim")]
    [TestCase("fdy_siege")]
    [TestCase("fdy_hunter")]
    [TestCase("fdy_overseer")]
    [TestCase("fdy_foreman")]
    [TestCase("fdy_inspector")]
    public void RangedDroids_UseTheFiringAnimationTheirModelProvides(string resref)
    {
        var root = FindRepositoryRoot();
        var configuredHaks = Environment.GetEnvironmentVariable("SWLOR_HAKS_CORPUS");
        var haks = string.IsNullOrWhiteSpace(configuredHaks) ? Path.Combine(root, "SWLOR_Haks") : configuredHaks;
        using var creature = Load("utc", resref);
        var rightHand = creature.RootElement.GetProperty("Equip_ItemList").GetProperty("value")
            .EnumerateArray().Single(item => item.GetProperty("__struct_id").GetInt32() == 16);
        using var weapon = Load("uti", rightHand.GetProperty("EquippedRes").GetProperty("value").GetString()!);
        var baseItem = (BaseItem)Number(weapon.RootElement, "BaseItem");

        baseItem.Should().Be(BaseItem.Rifle, "these creature models fire through the native crossbow animation");
        PistolAnimationRemap.ShouldUseFormerPistolAttackAnimation(baseItem, null).Should().BeFalse();
        PistolBaseItemCompatibility.GetCanonicalBaseItem(baseItem, resref + "_wp").Should().Be(baseItem);
        weapon.RootElement.GetProperty("PropertiesList").GetProperty("value").EnumerateArray()
            .Should().Contain(property => Number(property, "PropertyName") == (int)ItemPropertyType.UnlimitedAmmunition);

        var appearances = TwoDAReader.Read(Path.Combine(haks, "sw_2da", "appearance.2da"));
        var modelName = appearances.GetValue(Number(creature.RootElement, "Appearance_Type"), "RACE");
        var modelPath = Directory.EnumerateFiles(Path.Combine(haks, "sw_cr_creature"), "*.mdl")
            .Single(path => Path.GetFileNameWithoutExtension(path).Equals(modelName, StringComparison.OrdinalIgnoreCase));
        var model = new MdlReader().Parse(File.ReadAllBytes(modelPath));
        model.Animations.Should().Contain(animation => animation.Name.Equals("xbowshot", StringComparison.OrdinalIgnoreCase),
            $"{resref}'s model {modelName} must supply a firing clip instead of falling back to melee");

        JsonDocument Load(string type, string name) =>
            JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "Module", type, $"{name}.{type}.json")));
    }

    private static int Number(JsonElement element, string field) => element.GetProperty(field).GetProperty("value").GetInt32();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
