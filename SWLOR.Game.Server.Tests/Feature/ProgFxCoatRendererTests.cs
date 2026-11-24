using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class ProgFxCoatRendererTests
{
    [Test]
    public void CoatTextures_MatchTheHakAtlasCellOrder()
    {
        var manifest = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "tools", "ProgFxCoatAtlas.json");
        using var document = JsonDocument.Parse(File.ReadAllText(manifest));
        var cells = document.RootElement.GetProperty("cells").EnumerateArray()
            .Select(cell => cell.GetProperty("texture").GetString())
            .ToList();

        cells.Should().Equal(ProgFxCoatRenderer.CoatTextures,
            "the shader picks the atlas cell from the server's coat number");
    }

    [Test]
    public void EveryCoatingVisualEffect_HasAnAtlasCell()
    {
        var root = FindRepositoryRoot();
        var visualEffects = Test2daHelper.Read2da(new FileInfo(Path.Combine(root, "SWLOR_Haks", "sw_2da", "visualeffects.2da")));
        var progFx = Test2daHelper.Read2da(new FileInfo(Path.Combine(root, "SWLOR_Haks", "sw_2da", "progfx.2da")));

        var missing = visualEffects
            .Where(row => int.TryParse(row.Value.GetValueOrDefault("ProgFX_Duration"), out var id) &&
                          progFx.TryGetValue(id, out var effect) && effect.GetValueOrDefault("Type") == "1")
            .Select(row => (Row: row.Key, ProgFx: progFx[int.Parse(row.Value["ProgFX_Duration"])]))
            .Where(entry => ProgFxCoatRenderer.GetCoatForProgFxRow(entry.ProgFx["Type"], entry.ProgFx["Param1"]) == 0)
            .Select(entry => $"{entry.Row}: {entry.ProgFx["Param1"]}")
            .ToList();

        missing.Should().BeEmpty("every environment-map coating needs a progfx_coat cell");
    }

    [Test]
    public void ShadowShield_UsesTheShadeCoat()
    {
        var root = FindRepositoryRoot();
        var visualEffects = Test2daHelper.Read2da(new FileInfo(Path.Combine(root, "SWLOR_Haks", "sw_2da", "visualeffects.2da")));
        var progFx = Test2daHelper.Read2da(new FileInfo(Path.Combine(root, "SWLOR_Haks", "sw_2da", "progfx.2da")));
        var shadowShield = visualEffects[14];
        var row = progFx[int.Parse(shadowShield["ProgFX_Duration"])];

        shadowShield["Label"].Should().Be("VFX_DUR_PROT_SHADOW_ARMOR");
        ProgFxCoatRenderer.GetCoatForProgFxRow(row["Type"], row["Param1"])
            .Should().Be(ProgFxCoatRenderer.CoatTextures.ToList().IndexOf("vdu_tex_shade") + 1);
    }

    [TestCase("vdu_tex_stone", 1)]
    [TestCase("VDU_TEX_GRSTONE", 2)]
    [TestCase(" vdu_tex_ice ", 5)]
    [TestCase("vdu_tex_other", 0)]
    [TestCase("****", 0)]
    [TestCase(null, 0)]
    public void GetCoatForTexture_IsTheOneBasedAtlasCell(string texture, int expected)
    {
        ProgFxCoatRenderer.GetCoatForTexture(texture).Should().Be(expected);
    }

    [TestCase("1", "vdu_tex_bark", 3)]
    [TestCase("11", "vdu_tex_bark", 0)]
    [TestCase("****", "vdu_tex_bark", 0)]
    public void GetCoatForProgFxRow_OnlyCoatsEnvironmentMapRows(string type, string texture, int expected)
    {
        ProgFxCoatRenderer.GetCoatForProgFxRow(type, texture).Should().Be(expected);
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    public void LodRefreshChangesTheUniformWithoutChangingTheCoatingCell(int coat)
    {
        var first = ProgFxCoatRenderer.GetShaderCoat(coat, 0);
        var next = ProgFxCoatRenderer.GetShaderCoat(coat, 1);
        first.Should().Be(coat);
        next.Should().NotBe(first);
        ((int)MathF.Floor(next + 0.5f)).Should().Be(coat);
        (next > 0.5f).Should().Be(coat > 0);
        ProgFxCoatRenderer.GetShaderCoat(coat, 2).Should().Be(first);
    }

    [Test]
    public void GetCoatedMaterials_AddsTheCoatOnlyMaterialsOfRenderedModels()
    {
        var chest = new TintMapMaterialSelection(
            "pmd0_chest316",
            new TintMapMaterialDefinition("pmh0_chest316", "pmh0_chest316", TintMapLayerType.Metal1),
            1, 1, true, AppearanceArmor.Torso);
        var forearm = new TintMapMaterialSelection(
            "pmd0_forer282",
            new TintMapMaterialDefinition("troopbracer", "troopbracer", TintMapLayerType.Leather2),
            1, 1, true, AppearanceArmor.RightForearm);
        var coatOnly = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["PMD0_CHEST316"] = new[] { "havoc_box" },
            ["pmh0_chest316"] = new[] { "unrendered" },
        };

        ProgFxCoatRenderer.GetCoatedMaterials(new[] { chest, forearm, chest }, coatOnly)
            .Should().Equal("pmh0_chest316", "troopbracer", "havoc_box");
    }

    [Test]
    public void CoatOnlyMaterials_AreTintedModelsAndCoatCapableMaterials()
    {
        var root = FindRepositoryRoot();
        var tintModels = Test2daHelper.Read2da(new FileInfo(Path.Combine(root, "SWLOR_Haks", "sw_2da", "tintmap.2da"))).Values
            .Select(row => row.GetValueOrDefault("MODEL"))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows = Test2daHelper.Read2da(new FileInfo(Path.Combine(root, "SWLOR_Haks", "sw_2da", "progfxcoat.2da"))).Values;

        rows.Should().NotBeEmpty();
        foreach (var row in rows)
        {
            // Only models with tint selections reach the coat writer.
            tintModels.Should().Contain(row["MODEL"]);
            var material = Directory.GetDirectories(Path.Combine(root, "SWLOR_Haks"), "sw_*")
                .SelectMany(hak => Directory.GetFiles(hak, row["MATERIAL"] + ".mtr"))
                .Should().ContainSingle().Subject;
            File.ReadAllLines(material).Should().Contain(new[] { "texture8 progfx_coat", "parameter float progFxCoat 0.0" });
        }
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }
}
