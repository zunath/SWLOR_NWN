using System.Numerics;
using System.Text.Json;
using FluentAssertions;
using Nwn.Formats.NativeModels;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// The Tusken Gaderffii is quarterstaff part 62 (model 6, color 2) in every role. The October 2026 weapon revamp
/// replaced that bottom part with a copy of the plain staff part 53, so Tuskens carried a standard staff.
/// </summary>
public class GaderffiiAppearanceTests
{
    private const int Part = 62;
    private const string Texture = "sfg_ghaffia";

    private static readonly string[] GaderffiiBlueprints =
    {
        "sandstorm_gaffi", "tuskenmelee_wp", "tuskenelitem_wp", "tuskchampion_wp", "tuskwarcaller_wp", "tuskwarlord_wp", "drakebruiser_wp",
    };

    [Test]
    public void TuskenAndLootGaderffiis_UseTheGaderffiiInEveryRole()
    {
        var uti = Path.Combine(FindRepositoryRoot(), "Module", "uti");
        var named = Directory.EnumerateFiles(uti, "*.uti.json")
            .Where(path =>
            {
                using var blueprint = JsonDocument.Parse(File.ReadAllText(path));
                return Number(blueprint.RootElement, "BaseItem") == (int)BaseItem.QuarterStaff &&
                       blueprint.RootElement.GetProperty("LocalizedName").GetRawText().Contains("Gaderffii", StringComparison.OrdinalIgnoreCase);
            })
            .Select(path => Path.GetFileName(path).Replace(".uti.json", ""));
        named.Should().BeSubsetOf(GaderffiiBlueprints, "every Gaderffii staff blueprint is covered here");

        foreach (var resref in GaderffiiBlueprints)
        {
            using var blueprint = JsonDocument.Parse(File.ReadAllText(Path.Combine(uti, resref + ".uti.json")));
            var root = blueprint.RootElement;
            Number(root, "BaseItem").Should().Be((int)BaseItem.QuarterStaff, resref);
            foreach (var field in new[] { "ModelPart1", "ModelPart2", "ModelPart3", "xModelPart1", "xModelPart2", "xModelPart3" })
                Number(root, field).Should().Be(Part, $"{resref} {field}");
        }
    }

    [Test]
    public void QuarterstaffCatalog_OffersTheGaderffiiInEveryRole()
    {
        var catalog = new QuarterstaffAppearanceDefinition();
        // The editor numbers a part color * 100 + model; blueprints save model * 10 + color.
        const int catalogId = Part % 10 * 100 + Part / 10;
        QuarterstaffAppearanceDefinition.GaderffiiPart.Should().Be(catalogId);
        catalog.BottomParts.Should().Contain(catalogId);
        catalog.MiddleParts.Should().Contain(catalogId);
        catalog.TopParts.Should().Contain(catalogId);
    }

    [Test]
    public void GaderffiiParts_DrawTheGaderffiiOnTheNativeStaffJoints()
    {
        var weapons = Path.Combine(FindRepositoryRoot(), "SWLOR_Haks", "sw_weapon");
        if (!File.Exists(Path.Combine(weapons, "wdbqs_b_062.mdl")))
            Assert.Ignore("SWLOR_Haks sw_weapon is not checked out");

        // Native quarterstaff roles: bottom is the grip, middle both end bands, top both tips.
        var bottom = Vertices(weapons, 'b');
        bottom.Min(v => v.Y).Should().BeApproximately(-0.3f, 1e-4f);
        bottom.Max(v => v.Y).Should().BeApproximately(0.9f, 1e-4f);

        var middle = Vertices(weapons, 'm');
        middle.Should().OnlyContain(v => v.Y >= -0.6001f && v.Y <= -0.2999f || v.Y >= 0.8999f && v.Y <= 1.2001f);
        middle.Should().Contain(v => v.Y < 0).And.Contain(v => v.Y > 0.9f, "both end bands");

        var top = Vertices(weapons, 't');
        top.Should().OnlyContain(v => v.Y <= -0.5999f || v.Y >= 1.1999f);
        top.Min(v => v.Y).Should().BeLessThan(-0.85f, "the butt spike");
        top.Where(v => v.Y > 1.2f).Min(v => v.Z).Should().BeLessThan(-0.19f, "the hammer head");

        foreach (var role in "bmt")
        {
            var icon = Path.Combine(weapons, $"iwdbqs_{role}_062");
            var dds = File.ReadAllBytes(icon + ".dds");
            System.Text.Encoding.ASCII.GetString(dds, 84, 4).Should().Be("DXT5");
            (BitConverter.ToInt32(dds, 16), BitConverter.ToInt32(dds, 12)).Should().Be((64, 256), "the quarterstaff icon canvas");
            dds.Should().HaveCount(128 + 16 * 16 * 64, "a single-level inventory icon");
            File.ReadAllBytes(icon + ".tga").Should().HaveCount(18 + 8 * 8 * 4, "the 8x8 registration stub");
        }
        File.ReadAllBytes(Path.Combine(weapons, "iwdbqs_b_062.dds"))
            .Should().NotEqual(File.ReadAllBytes(Path.Combine(weapons, "iwdbqs_b_053.dds")), "the Gaderffii has its own artwork");
    }

    private static List<Vector3> Vertices(string weapons, char role)
    {
        var model = new MdlReader().Parse(File.ReadAllBytes(Path.Combine(weapons, $"wdbqs_{role}_062.mdl")));
        var meshes = model.GetMeshNodes().OfType<MdlTrimeshNode>().Where(node => node.Vertices.Any()).ToList();
        meshes.Should().NotBeEmpty();
        meshes.Should().OnlyContain(node => string.Equals(node.Bitmap, Texture, StringComparison.OrdinalIgnoreCase),
            $"part {role} keeps the Gaderffii texture");
        return meshes.SelectMany(node => node.Vertices.Select(vertex => ToModelSpace(node, vertex))).ToList();
    }

    private static Vector3 ToModelSpace(MdlNode node, Vector3 vertex)
    {
        for (var current = node; current != null; current = current.Parent)
            vertex = Vector3.Transform(vertex, current.Orientation) + current.Position;
        return vertex;
    }

    private static int Number(JsonElement root, string field) => root.GetProperty(field).GetProperty("value").GetInt32();

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;
        directory.Should().NotBeNull();
        return directory!.FullName;
    }
}
