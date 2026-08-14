using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class PlayerAbilityRadialMenuTests
{
    private const int GeneratedFeatStart = 2000;
    private const int GeneratedFeatEnd = 2717;

    private static readonly HashSet<FeatType> ManualHotbarFeats =
    [
        FeatType.ForceJudgment1,
        FeatType.ForceJudgment2,
        FeatType.ForceJudgment3,
        FeatType.ForceBurst1,
        FeatType.PurifyingWave1,
        FeatType.RadiantLance1,
        FeatType.RadiantLance2,
        FeatType.RadiantLance3
    ];

    [Test]
    public void CustomPlayerAbilityFeats_AreLinkedAndAvailableOnFighterMenu()
    {
        var root = FindRepositoryRoot();
        var featRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var classFeatRows = Read2da(root / "SWLOR_Haks" / "sw_2da" / "CLS_FEAT_FIGHT.2da");
        var playerAbilityFeats = BuildPlayerAbilityFeats()
            .Where(feat => (int)feat >= GeneratedFeatStart)
            .OrderBy(feat => (int)feat)
            .ToArray();
        var playerAbilityFeatIds = playerAbilityFeats
            .Select(feat => (int)feat)
            .ToHashSet();
        var npcAbilityFeatIds = BuildNpcAbilityFeats()
            .Select(feat => (int)feat)
            .ToHashSet();
        // Mimicry trait techniques are passive: they are granted (and appear in the class feat
        // table) like other techniques but are not cast, so they intentionally have no spells.2da
        // link. Their passiveness is validated by MimicryTests instead.
        var mimicryTraitFeatIds = BuildMimicryTraitFeatIds();
        var failures = new List<string>();

        playerAbilityFeats.Should().NotBeEmpty();

        foreach (var feat in playerAbilityFeats)
        {
            var featId = (int)feat;
            featRows.Should().ContainKey(featId, $"{feat} must exist in feat.2da");

            var featRow = featRows[featId];
            var featLabel = featRow["LABEL"];

            if (!mimicryTraitFeatIds.Contains(featId))
            {
                featRow["SPELLID"].Should().NotBe("****", $"{feat} must link to spells.2da");

                var spellId = int.Parse(featRow["SPELLID"]);
                spellRows.Should().ContainKey(spellId, $"{feat} must have a spell row");
                spellRows[spellId]["Label"].Should().Be(featLabel, $"{feat} spell row should use the feat.2da label");
                spellRows[spellId]["FeatID"].Should().Be(featId.ToString(), $"{feat} spell row should point back to its feat");
            }

            var classFeatEntry = classFeatRows
                .Should()
                .ContainSingle(row => row.Value["FeatIndex"] == featId.ToString(), $"{feat} must be available to the fighter class radial menu")
                .Which;
            var classFeatRow = classFeatEntry.Value;

            classFeatRow["FeatLabel"].Should().Be(featLabel);
            classFeatRow["List"].Should().Be("1");
            classFeatRow["GrantedOnLevel"].Should().Be("99");
            classFeatRow["OnMenu"].Should().Be("1");
            if (ManualHotbarFeats.Contains(feat))
            {
                classFeatEntry.Key.Should().BeLessThan(1024, $"{feat} must be within the class feat rows scanned for manual hotbar selection");
            }
        }

        foreach (var (rowNumber, row) in classFeatRows.OrderBy(row => row.Key))
        {
            if (!row.TryGetValue("FeatIndex", out var featIndexValue) ||
                !int.TryParse(featIndexValue, out var featIndex) ||
                featIndex < GeneratedFeatStart ||
                featIndex > GeneratedFeatEnd)
            {
                continue;
            }

            var isVisible = row.GetValueOrDefault("List") == "1" ||
                            row.GetValueOrDefault("OnMenu") == "1";
            if (isVisible && !playerAbilityFeatIds.Contains(featIndex))
            {
                failures.Add($"CLS_FEAT_FIGHT row {rowNumber} exposes stale generated feat {featIndex} ({row.GetValueOrDefault("FeatLabel")}).");
            }
        }

        foreach (var (featId, row) in featRows.OrderBy(row => row.Key))
        {
            if (featId < GeneratedFeatStart || featId > GeneratedFeatEnd)
                continue;

            if (playerAbilityFeatIds.Contains(featId))
                continue;
            if (npcAbilityFeatIds.Contains(featId))
                continue;

            var spellId = row.GetValueOrDefault("SPELLID");
            if (string.IsNullOrWhiteSpace(spellId) || spellId == "****")
                continue;

            if (!int.TryParse(spellId, out var linkedSpellId) || !spellRows.TryGetValue(linkedSpellId, out var spellRow))
            {
                failures.Add($"feat.2da row {featId} ({row.GetValueOrDefault("LABEL")}) has invalid legacy SPELLID {spellId}.");
                continue;
            }

            spellRow["Label"].Should().Be(row.GetValueOrDefault("LABEL"));
            spellRow["IconResRef"].Should().Be(row.GetValueOrDefault("ICON"));
            spellRow["FeatID"].Should().Be(featId.ToString());
        }

        failures.Should().BeEmpty(string.Join(Environment.NewLine, failures));
    }

    private static HashSet<FeatType> BuildPlayerAbilityFeats()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var feats = new HashSet<FeatType>();
        var definitions = definitionType.Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                definitionType.IsAssignableFrom(type) &&
                type.Namespace != "SWLOR.Game.Server.Feature.AbilityDefinition.NPC")
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!);

        foreach (var definition in definitions)
        {
            foreach (var feat in definition.BuildAbilities().Keys)
            {
                feats.Add(feat);
            }
        }

        return feats;
    }

    private static HashSet<int> BuildMimicryTraitFeatIds()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var ids = new HashSet<int>();
        var definitions = definitionType.Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                definitionType.IsAssignableFrom(type) &&
                type.Namespace == "SWLOR.Game.Server.Feature.AbilityDefinition.Mimicry")
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!);

        foreach (var definition in definitions)
        {
            foreach (var (feat, detail) in definition.BuildAbilities())
            {
                if (detail.IsMimicryTrait)
                    ids.Add((int)feat);
            }
        }

        return ids;
    }

    private static HashSet<FeatType> BuildNpcAbilityFeats()
    {
        var definitionType = typeof(IAbilityListDefinition);
        var feats = new HashSet<FeatType>();
        var definitions = definitionType.Assembly
            .GetTypes()
            .Where(type =>
                type.IsClass &&
                !type.IsAbstract &&
                definitionType.IsAssignableFrom(type) &&
                type.Namespace == "SWLOR.Game.Server.Feature.AbilityDefinition.NPC")
            .Select(type => (IAbilityListDefinition)Activator.CreateInstance(type)!);

        foreach (var definition in definitions)
        {
            foreach (var feat in definition.BuildAbilities().Keys)
            {
                feats.Add(feat);
            }
        }

        return feats;
    }

    private static Dictionary<int, Dictionary<string, string>> Read2da(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines
            .First(line => line.TrimStart().Length > 0 && !char.IsDigit(line.TrimStart()[0]) && !line.StartsWith("2DA"))
            .Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines)
        {
            var cells = line.Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(cells[0], out var row))
                continue;

            var values = new Dictionary<string, string>();
            for (var index = 0; index < header.Length && index + 1 < cells.Length; index++)
            {
                values[header[index]] = cells[index + 1];
            }

            result[row] = values;
        }

        return result;
    }

    private static PathInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = directory.FullName;
            if (File.Exists(Path.Combine(candidate, "SWLOR.Game.Server.sln")) &&
                File.Exists(Path.Combine(candidate, "SWLOR_Haks", "sw_2da", "feat.2da")))
            {
                return new PathInfo(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }

    private sealed record PathInfo(string FullName)
    {
        public static PathInfo operator /(PathInfo path, string child)
        {
            return new PathInfo(Path.Combine(path.FullName, child));
        }
    }
}
