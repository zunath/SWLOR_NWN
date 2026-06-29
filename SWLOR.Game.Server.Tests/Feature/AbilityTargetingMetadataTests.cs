using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Feature;

public class AbilityTargetingMetadataTests
{
    [Test]
    public void PositiveDelayCastedAreaAbilities_DeclareTargetingMetadata()
    {
        var abilities = BuildAbilityDetails();
        var positiveDelayAreaAbilities = FindPositiveDelayCastedAreaAbilities(abilities);

        var missing = positiveDelayAreaAbilities
            .Where(entry => entry.Ability.Targeting == null)
            .Select(entry => entry.Feat.ToString())
            .OrderBy(feat => feat)
            .ToArray();

        missing.Should().BeEmpty("positive-delay casted area abilities need targeting metadata for cast-time previews");
    }

    [Test]
    public void PositiveDelayCastedAreaAbilityTargeting_Matches2daShapeAndSize()
    {
        var root = FindRepositoryRoot();
        var abilities = BuildAbilityDetails();
        var featRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "feat.2da");
        var spellRows = Read2daRows(root / "SWLOR_Haks" / "sw_2da" / "spells.2da");
        var positiveDelayAreaAbilities = FindPositiveDelayCastedAreaAbilities(abilities);

        foreach (var (feat, ability) in positiveDelayAreaAbilities)
        {
            if (!featRows.TryGetValue((int)feat, out var featRow))
                continue;

            if (!int.TryParse(featRow["SPELLID"], out var spellId))
                continue;

            if (!spellRows.TryGetValue(spellId, out var spellRow))
                continue;

            if (spellRow["TargetShape"] == "****")
                continue;

            var targeting = ability.Targeting;
            targeting.Should().NotBeNull($"{feat} declares a 2DA targeting shape");
            targeting!.UpdatesClientTargeting.Should().BeTrue($"{feat} declares a 2DA targeting shape");
            targeting!.Spell.Should().Be((Spell)spellId);
            targeting.Shape.Should().Be(MapShape(spellRow["TargetShape"]));
            targeting.SizeX.Should().Be(ParseSize(spellRow["TargetSizeX"]));
            targeting.SizeY.Should().Be(ParseSize(spellRow["TargetSizeY"]));

            if (int.TryParse(spellRow["TargetFlags"], out var targetFlags) &&
                (targetFlags & (int)AbilityTargetingFlags.OriginOnSelf) != 0)
            {
                targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf).Should().BeTrue();
            }

            if (ability.IsHostileAbility)
            {
                targeting.Flags.HasFlag(AbilityTargetingFlags.HarmsEnemies).Should().BeTrue();
            }
            else
            {
                targeting.Flags.HasFlag(AbilityTargetingFlags.HelpsAllies).Should().BeTrue();
            }
        }
    }

    private static Dictionary<FeatType, AbilityDetail> BuildAbilityDetails()
    {
        var definitions = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) &&
                           !type.IsInterface &&
                           !type.IsAbstract);

        var abilities = new Dictionary<FeatType, AbilityDetail>();
        foreach (var definitionType in definitions)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType, nonPublic: true)!;
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                abilities[feat] = ability;
            }
        }

        return abilities;
    }

    private static IEnumerable<(FeatType Feat, AbilityDetail Ability)> FindPositiveDelayCastedAreaAbilities(
        IReadOnlyDictionary<FeatType, AbilityDetail> abilities)
    {
        return abilities
            .Where(entry =>
                entry.Value.ActivationType == AbilityActivationType.Casted &&
                entry.Value.IsAreaAbility &&
                GetActivationDelay(entry.Value) > 0f)
            .Select(entry => (entry.Key, entry.Value));
    }

    private static float GetActivationDelay(AbilityDetail ability)
    {
        return ability.ActivationDelay?.Invoke(0, 0, ability.AbilityLevel) ?? 0f;
    }

    private static AbilityTargetingShapeType MapShape(string shape)
    {
        return shape switch
        {
            "sphere" => AbilityTargetingShapeType.Sphere,
            "cone" => AbilityTargetingShapeType.Cone,
            "rectangle" => AbilityTargetingShapeType.Rect,
            _ => throw new InvalidOperationException($"Unsupported targeting shape '{shape}'.")
        };
    }

    private static float ParseSize(string value)
    {
        return value == "****"
            ? 0f
            : float.Parse(value, CultureInfo.InvariantCulture);
    }

    private static Dictionary<int, Dictionary<string, string>> Read2daRows(PathInfo path)
    {
        var lines = File.ReadAllLines(path.FullName)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var header = lines[1].Split((char[])null!, StringSplitOptions.RemoveEmptyEntries);
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var line in lines.Skip(2))
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
