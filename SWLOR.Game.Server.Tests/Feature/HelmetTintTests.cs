using System.Text.RegularExpressions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Feature;

public class HelmetTintTests
{
    [TestCase("helm_114", true, true, 1114)]
    [TestCase("HELM_114", true, true, 1114)]
    [TestCase("helm_034", true, true, 1034)]
    [TestCase("helm_114", false, true, 121)]
    [TestCase("helm_114", true, false, 121)]
    [TestCase("helm_000", true, true, 121)]
    [TestCase("helm_1140", true, true, 121)]
    [TestCase("helmet_114", true, true, 121)]
    [TestCase(null, true, true, 121)]
    public void HelmetRenderingRestoresTheCanonicalHeadWhenHiddenUnequippedOrUnsupported(
        string model, bool visible, bool parts, int expected)
    {
        Assert.That(HelmetModelRenderer.ResolveHead(model, 121, visible, parts), Is.EqualTo(expected));
    }

    [TestCase(1000, false)]
    [TestCase(1001, true)]
    [TestCase(1999, true)]
    [TestCase(2000, false)]
    [TestCase(306, false)]
    public void OnlyTheReservedHelmetRangeIsTreatedAsARenderHead(int head, bool rendered)
    {
        Assert.That(HelmetModelRenderer.IsRenderedHead((ushort)head), Is.EqualTo(rendered));
    }

    [TestCase('m', "H", 1114, "pmh0_head1114")]
    [TestCase('f', "h", 1114, "pfh0_head1114")]
    [TestCase('m', "E", 1002, "pme0_head1002")]
    public void RenderHeadModelsUsePhenotypeZeroForTheWearersRaceAndGender(char gender, string race, int head, string expected)
    {
        Assert.That(HelmetModelRenderer.GetHeadModel(gender, race, (ushort)head), Is.EqualTo(expected));
    }

    [Test]
    public void EveryTintableHelmetShipsRenderHeadsForEveryPlayableSpecies()
    {
        var root = FindRepositoryRoot();
        var appearanceRows = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "appearance.2da"))
            .Select(line => Regex.Matches(line, "\"[^\"]*\"|\\S+").Select(match => match.Value).ToArray())
            .ToList();
        var header = appearanceRows[2];
        var raceColumn = Array.IndexOf(header, "RACE") + 1;
        var rows = appearanceRows.Skip(3).Where(columns => columns.Length > raceColumn && int.TryParse(columns[0], out _))
            .ToDictionary(columns => int.Parse(columns[0]), columns => columns[raceColumn].ToLowerInvariant());
        var races = RacialAppearanceRegistry.GetAppearanceTypes()
            .Select(type => rows[(int)type])
            .Distinct()
            .ToList();
        Assert.That(races, Does.Contain("h"));

        var helmets = File.ReadAllLines(Path.Combine(root, "SWLOR_Haks", "sw_2da", "tintmap.2da"))
            .Select(line => Regex.Split(line.Trim(), @"\s+"))
            .Where(columns => columns.Length == 4 && Regex.IsMatch(columns[1], @"^helm_\d{3}$"))
            .Select(columns => columns[1])
            .Distinct()
            .ToList();
        Assert.That(helmets, Is.Not.Empty);

        var heads = Path.Combine(root, "SWLOR_Haks", "sw_pt_head");
        foreach (var helmet in helmets)
        {
            var head = HelmetModelRenderer.ResolveHead(helmet, 0, true, true);
            foreach (var race in races)
            foreach (var gender in new[] { 'm', 'f' })
            {
                var model = HelmetModelRenderer.GetHeadModel(gender, race, head);
                Assert.That(File.Exists(Path.Combine(heads, model + ".mdl")), Is.True,
                    $"{helmet} needs {model}.mdl; add race '{race}' to BODY_RACES in GenerateHelmetRgbModels.py");
            }
        }
    }

    [TestCase("helm_114", 1u, 2u, true)]
    [TestCase("HELM_114", 1u, 2u, true)]
    [TestCase("helm_114", 1u, 1u, true)]
    [TestCase("pfh0_head103", 2u, 2u, true)]
    [TestCase("pfh0_chest249", 1u, 2u, true)]
    public void RobeRgbAvailabilityIgnoresNonRobeAttachments(string model, uint item, uint creature, bool supported)
    {
        var selection = new TintMapMaterialSelection(model,
            new TintMapMaterialDefinition(model, model, new[] { TintMapLayerType.Cloth1 }),
            item, creature, true, AppearanceArmor.Invalid);
        Assert.That(RobeModelRenderer.SupportsRgb(selection), Is.EqualTo(supported));
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
                return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate SWLOR_NWN repository root.");
    }
}
