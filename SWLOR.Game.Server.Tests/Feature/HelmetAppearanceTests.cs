using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetAppearanceTests
{
    private const int HelmetBaseItem = 17;
    private const int DarthRevanMaskModel = 223;

    [Test]
    public void DarthRevanMaskModel_IsUsedOnlyByDarthRevansMask()
    {
        var users = new List<string>();
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root(), "Module", "uti"), "*.uti.json"))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(path).TrimStart('﻿'));
            var data = json.RootElement;
            if (data.GetProperty("BaseItem").GetProperty("value").GetInt32() != HelmetBaseItem) continue;

            var model = data.TryGetProperty("xModelPart1", out var extended)
                ? extended.GetProperty("value").GetInt32()
                : data.GetProperty("ModelPart1").GetProperty("value").GetInt32();
            if (model == DarthRevanMaskModel)
                users.Add(Path.GetFileName(path).Replace(".uti.json", ""));
        }

        users.Should().BeEquivalentTo(new[] { "revan_mask" },
            "helmet 223 is Darth Revan's mask and hood, not a generic visor appearance");
    }

    private static string Root()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "SWLOR.Game.Server.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("Repository root not found.");
    }
}
