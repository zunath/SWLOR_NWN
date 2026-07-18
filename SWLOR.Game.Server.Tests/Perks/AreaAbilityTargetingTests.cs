using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Enforces the aimed-vs-self-centered area targeting rule from AGENTS.md by reflecting over every
/// ability definition and cross-checking feat.2da. Deliberately has no hand-maintained ability list:
/// a new area ability is covered the moment it is defined.
///
/// Aimed areas ("in a line" / "in a cone") let the player choose a direction, so they present a
/// targeting cursor: feat.2da TARGETSELF blank + HostileFeat=1, exactly like Earthshatter.
/// Self-centered areas ("within Nm") always originate on the caster and must not prompt:
/// TARGETSELF=1 + HostileFeat cleared.
/// </summary>
public class AreaAbilityTargetingTests
{
    private static readonly AbilityTargetingShapeType[] AimedShapes =
    {
        AbilityTargetingShapeType.Rect,
        AbilityTargetingShapeType.Cone
    };

    [Test]
    public void AreaAbilities_DeclaringAShape_AlsoDeclareATargetingSpell()
    {
        // ApplyTargetingMetadata drops all client targeting when the spell is Invalid, which costs
        // the ability its cursor AND its ground area marker with no error anywhere. Every rank needs
        // its own spells.2da row, not just rank one.
        var offenders = GetTargetedAbilities()
            .Where(x => x.Targeting.UpdatesClientTargeting)
            .Where(x => x.Targeting.Shape != AbilityTargetingShapeType.None)
            .Where(x => x.Targeting.Spell == Spell.Invalid)
            .Select(x => $"{x.DefinitionName}.{x.Feat} ({x.Targeting.Shape})")
            .ToList();

        offenders.Should().BeEmpty(
            "every rank of an area ability needs its own spells.2da row and Spell enum value; " +
            "Spell.Invalid silently disables the targeting cursor and area marker");
    }

    [Test]
    public void AimedAreaAbilities_TakeATargetingCursorInFeat2da()
    {
        var feats = ReadFeat2da();
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = new List<string>();
        foreach (var ability in GetTargetedAbilities()
                     .Where(x => x.IsHostile && x.IsArea)
                     .Where(x => playerFeats.Contains(x.Feat))
                     .Where(x => AimedShapes.Contains(x.Targeting.Shape)))
        {
            if (!feats.TryGetValue(ability.Feat.ToString(), out var row))
                continue;

            if (row.TargetSelf != "****" || row.HostileFeat != "1")
            {
                offenders.Add(
                    $"{ability.Feat} ({ability.Targeting.Shape}) has TARGETSELF={row.TargetSelf} " +
                    $"HostileFeat={row.HostileFeat}, expected TARGETSELF=**** HostileFeat=1");
            }
        }

        offenders.Should().BeEmpty(
            "aimed line/cone areas let the player choose a direction, so they must present a " +
            "targeting cursor like Earthshatter");
    }

    [Test]
    public void SelfCenteredAreaAbilities_DoNotPromptForATargetInFeat2da()
    {
        var feats = ReadFeat2da();
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = new List<string>();
        foreach (var ability in GetTargetedAbilities()
                     .Where(x => x.IsHostile && x.IsArea)
                     .Where(x => playerFeats.Contains(x.Feat))
                     .Where(x => x.Targeting.Shape == AbilityTargetingShapeType.Sphere)
                     .Where(x => x.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf)))
        {
            if (!feats.TryGetValue(ability.Feat.ToString(), out var row))
                continue;

            if (row.TargetSelf != "1")
            {
                offenders.Add(
                    $"{ability.Feat} (self-centered Sphere) has TARGETSELF={row.TargetSelf}, expected 1");
            }
        }

        offenders.Should().BeEmpty(
            "self-centered radius areas always originate on the caster and must not prompt for a target");
    }

    [Test]
    public void SphereAreaAbilities_DeclareARadiusAndNoWidth()
    {
        var offenders = GetTargetedAbilities()
            .Where(x => x.Targeting.Shape == AbilityTargetingShapeType.Sphere)
            .Where(x => x.Targeting.SizeX <= 0f || x.Targeting.SizeY != 0f)
            .Select(x => $"{x.DefinitionName}.{x.Feat} (sizeX={x.Targeting.SizeX}, sizeY={x.Targeting.SizeY})")
            .ToList();

        offenders.Should().BeEmpty("a sphere uses sizeX as its radius and leaves sizeY at zero");
    }

    private sealed record TargetedAbility(
        string DefinitionName,
        FeatType Feat,
        AbilityTargetingDetail Targeting,
        bool IsArea,
        bool IsHostile);

    private static IEnumerable<TargetedAbility> GetTargetedAbilities()
    {
        var definitionTypes = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(x => typeof(IAbilityListDefinition).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
            .OrderBy(x => x.Name);

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                if (ability.Targeting == null)
                    continue;

                yield return new TargetedAbility(
                    definitionType.Name,
                    feat,
                    ability.Targeting,
                    ability.IsAreaAbility,
                    ability.IsHostileAbility);
            }
        }
    }

    /// <summary>
    /// Feats a player can actually buy. The cursor rules are about what a player clicks, so NPC and
    /// creature source feats (which the AI activates directly) are out of scope.
    /// </summary>
    private static HashSet<FeatType> GetPlayerGrantedFeats()
    {
        // PerkBuilder.Build() reads 2DAs through the NWN runtime, so perk definitions cannot be
        // instantiated in a unit test. Scan the definition sources for GrantsFeat instead.
        var perkDefinitionRoot = Path.Combine(
            FindRepositoryRoot().FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PerkDefinition");

        var feats = new HashSet<FeatType>();
        foreach (var file in Directory.EnumerateFiles(perkDefinitionRoot, "*.cs", SearchOption.AllDirectories))
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(file), @"GrantsFeat\(FeatType\.(\w+)\)"))
            {
                if (Enum.TryParse<FeatType>(match.Groups[1].Value, out var feat))
                    feats.Add(feat);
            }
        }

        feats.Should().NotBeEmpty("the perk definition sources should declare granted feats");

        return feats;
    }

    private sealed record Feat2daRow(string TargetSelf, string HostileFeat, string SpellId);

    private static Dictionary<string, Feat2daRow> ReadFeat2da()
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_2da", "feat.2da");
        var lines = File.ReadAllLines(path);
        var headers = lines[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Data rows carry a leading row-id column that the header row does not.
        var targetSelfIndex = Array.IndexOf(headers, "TARGETSELF") + 1;
        var hostileIndex = Array.IndexOf(headers, "HostileFeat") + 1;
        var spellIdIndex = Array.IndexOf(headers, "SPELLID") + 1;

        var rows = new Dictionary<string, Feat2daRow>();
        foreach (var line in lines.Skip(3))
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length <= Math.Max(targetSelfIndex, Math.Max(hostileIndex, spellIdIndex)))
                continue;

            var label = tokens[1];
            if (label == "****")
                continue;

            rows[label] = new Feat2daRow(
                tokens[targetSelfIndex],
                tokens[hostileIndex],
                tokens[spellIdIndex]);
        }

        return rows;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")) &&
                Directory.Exists(Path.Combine(directory.FullName, "SWLOR_Haks")))
            {
                return directory;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");
    }
}
