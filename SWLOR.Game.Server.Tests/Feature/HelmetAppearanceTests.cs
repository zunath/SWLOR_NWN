using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.MigrationDefinition;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetAppearanceTests
{
    private const int HelmetBaseItem = 17;
    private const int DarthRevanMaskModel = 223;

    [Test]
    public void DarthRevanMaskModel_IsUsedOnlyByDarthRevansMask()
    {
        var users = new List<string>();
        var module = Path.Combine(Root(), "Module");
        foreach (var path in Directory.EnumerateFiles(module, "*.json", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(path).TrimStart('﻿');
            if (!text.Contains("\"BaseItem\"")) continue;
            using var json = JsonDocument.Parse(text);
            FindReservedHelmets(json.RootElement, Path.GetRelativePath(module, path).Replace('\\', '/'), users);
        }

        users.Should().BeEquivalentTo(new[] { "uti/revan_mask.uti.json" },
            "helmet 223 is Darth Revan's mask and hood, not a generic visor appearance");
    }

    [Test]
    public void PlayerAppearanceChoicesAndMigrationFallbacks_ExcludeDarthRevanMask()
    {
        new GeneralArmorAppearanceDefinition().Helmet.Should().NotContain(DarthRevanMaskModel);
        new DroidArmorAppearanceDefinition().Helmet.Should().NotContain(DarthRevanMaskModel);
        HeadgearSlotMigration.LegacyHeadgear.Values.Should().NotContain(DarthRevanMaskModel);
    }

    private static void FindReservedHelmets(JsonElement data, string location, List<string> users)
    {
        if (data.ValueKind == JsonValueKind.Object)
        {
            if (data.TryGetProperty("BaseItem", out var baseItem) &&
                baseItem.GetProperty("value").GetInt32() == HelmetBaseItem &&
                new[] { "ModelPart1", "xModelPart1" }.Any(field =>
                    data.TryGetProperty(field, out var model) && model.GetProperty("value").GetInt32() == DarthRevanMaskModel))
                users.Add(location);

            foreach (var field in data.EnumerateObject())
                FindReservedHelmets(field.Value, location + "/" + field.Name, users);
        }
        else if (data.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var child in data.EnumerateArray())
                FindReservedHelmets(child, location + "/" + index++, users);
        }
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
