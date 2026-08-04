using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Enforces the aimed-vs-self-centered area targeting rule from AGENTS.md by reflecting over every
/// ability definition and cross-checking feat.2da. Deliberately has no hand-maintained ability list:
/// a new area ability is covered the moment it is defined.
///
/// Aimed areas (a line, cone, or placed sphere) let the player choose a direction or ground point,
/// so they present a targeting cursor: feat.2da TARGETSELF blank. Earthshatter and Adhesive Grenade
/// are hostile reference cases.
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
    public void HostileAreaAbilities_DeclareClientTargeting()
    {
        // The silent failure lives in ApplyTargetingMetadata: when the targeting spell is
        // Spell.Invalid it skips building targeting metadata entirely, so the ability ends up with
        // no Targeting at all - no cursor and no ground area marker, and nothing throws.
        // AbilityTargeting.ValidateTargeting only catches the opposite case (metadata that exists
        // but carries Spell.Invalid), so asserting on that alone would never fire. Assert the
        // invariant that actually breaks instead: a hostile area ability must have targeting.
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = GetAllAbilities()
            .Where(x => x.IsHostile && x.IsArea)
            .Where(x => playerFeats.Contains(x.Feat))
            .Where(x => !IsBeastActivated(x))
            .Where(x => x.Targeting is not { UpdatesClientTargeting: true })
            .Select(x => x.Feat.ToString())
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "every rank of an area ability needs its own spells.2da row and Spell enum value; " +
            "without one, ApplyTargetingMetadata silently produces no cursor and no area marker");
    }

    [Test]
    public void AreaAbilities_TargetingSpellMatchesTheFeatSpellId()
    {
        // feat.2da SPELLID is what the client actually resolves targeting through, so a C# ability
        // pointing at a different spells.2da row than its feat row silently targets using the wrong
        // shape and size.
        var feats = ReadFeat2da();
        var spellLabelsByRow = ReadSpellRowLabels();
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = new List<string>();
        foreach (var ability in GetTargetedAbilities()
                     .Where(x => x.Targeting.UpdatesClientTargeting)
                     .Where(x => playerFeats.Contains(x.Feat)))
        {
            if (!feats.TryGetValue(ability.Feat.ToString(), out var row))
            {
                offenders.Add($"{ability.Feat} has no feat.2da row");
                continue;
            }

            if (!spellLabelsByRow.TryGetValue(row.SpellId, out var spellLabel))
            {
                offenders.Add($"{ability.Feat} has SPELLID={row.SpellId}, which is not a spells.2da row");
                continue;
            }

            if (!string.Equals(spellLabel, ability.Targeting.Spell.ToString(), StringComparison.Ordinal))
            {
                offenders.Add(
                    $"{ability.Feat} targets Spell.{ability.Targeting.Spell} but its feat row " +
                    $"points at SPELLID={row.SpellId} ({spellLabel})");
            }
        }

        offenders.Should().BeEmpty("a feat's SPELLID must resolve to the same spell the ability targets with");
    }

    [Test]
    public void AimedAreaAbilities_TakeATargetingCursorInFeat2da()
    {
        var feats = ReadFeat2da();
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = new List<string>();
        foreach (var ability in GetTargetedAbilities()
                     .Where(x => x.IsArea)
                     .Where(x => playerFeats.Contains(x.Feat))
                     .Where(IsAimedArea))
        {
            if (!feats.TryGetValue(ability.Feat.ToString(), out var row))
            {
                offenders.Add($"{ability.Feat} has no feat.2da row");
                continue;
            }

            if (row.TargetSelf != "****")
            {
                offenders.Add(
                    $"{ability.Feat} ({ability.Targeting.Shape}) has TARGETSELF={row.TargetSelf} " +
                    $"HostileFeat={row.HostileFeat}, expected TARGETSELF=****");
            }
        }

        offenders.Should().BeEmpty(
            "aimed areas let the player choose a direction or ground point, so they must present a " +
            "targeting cursor like Earthshatter");
    }

    [Test]
    public void SelfCenteredAreaAbilities_DoNotPromptForATargetInFeat2da()
    {
        var feats = ReadFeat2da();
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = new List<string>();
        foreach (var ability in GetTargetedAbilities()
                     .Where(x => x.IsArea)
                     .Where(x => playerFeats.Contains(x.Feat))
                     .Where(x => x.Targeting.Shape == AbilityTargetingShapeType.Sphere)
                     .Where(x => x.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf)))
        {
            if (!feats.TryGetValue(ability.Feat.ToString(), out var row))
            {
                offenders.Add($"{ability.Feat} has no feat.2da row");
                continue;
            }

            if (row.TargetSelf != "1" || row.HostileFeat != "****")
            {
                offenders.Add(
                    $"{ability.Feat} (self-centered Sphere) has TARGETSELF={row.TargetSelf} " +
                    $"HostileFeat={row.HostileFeat}, expected TARGETSELF=1 HostileFeat=****");
            }
        }

        offenders.Should().BeEmpty(
            "self-centered radius areas always originate on the caster and must not prompt for a target");
    }

    [Test]
    public void SelfCenteredAreaAbilities_DoNotRequireATargetInCode()
    {
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = GetTargetedAbilities()
            .Where(x => x.IsHostile && x.IsArea && x.RequiresTarget)
            .Where(x => playerFeats.Contains(x.Feat))
            .Where(x => x.Targeting.Shape == AbilityTargetingShapeType.Sphere)
            .Where(x => x.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf))
            .Select(x => $"{x.DefinitionName}.{x.Feat}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "self-centered radius areas execute on the caster and must not call RequiresTarget()");
    }

    [Test]
    public void AimedAreaAbilities_UseExplicitObjectOrLocationTargeting()
    {
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = GetTargetedAbilities()
            .Where(x => x.IsArea)
            .Where(x => playerFeats.Contains(x.Feat))
            .Where(IsAimedArea)
            .Where(x => x.RequiresTarget == x.RequiresLocationTarget)
            .Select(x => $"{x.DefinitionName}.{x.Feat}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "aimed areas must deliberately require either a selected object or a location/direction, but never both; offenders: {0}",
            string.Join(", ", offenders));
    }

    [Test]
    public void AimedAreaAbilities_RequiringObjectsDeclareTheirRangeExplicitly()
    {
        var playerFeats = GetPlayerGrantedFeats();

        var offenders = GetTargetedAbilities()
            .Where(x => x.IsArea && x.RequiresTarget && !x.HasExplicitMaxRange)
            .Where(x => playerFeats.Contains(x.Feat))
            .Select(x => $"{x.DefinitionName}.{x.Feat}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        offenders.Should().BeEmpty(
            "object-targeted areas must deliberately declare their range instead of inheriting the 5m default; offenders: {0}",
            string.Join(", ", offenders));
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
        bool IsHostile,
        bool RequiresTarget,
        bool RequiresLocationTarget,
        bool HasExplicitMaxRange,
        SkillType SkillType,
        bool IsMimicryTechnique);

    /// <summary>
    /// Beast abilities originate on the companion, not the player, so there is nothing for the
    /// player to aim and they carry no client targeting by design. Keyed off the declared skill
    /// rather than a hand-maintained ability list.
    /// </summary>
    private static bool IsBeastActivated(TargetedAbility ability)
    {
        return ability.SkillType == SkillType.BeastMastery;
    }

    private static bool IsAimedArea(TargetedAbility ability)
    {
        return AimedShapes.Contains(ability.Targeting.Shape) ||
               ability.Targeting.Shape == AbilityTargetingShapeType.Sphere &&
               !ability.Targeting.Flags.HasFlag(AbilityTargetingFlags.OriginOnSelf);
    }

    /// <summary>Abilities that declare targeting metadata.</summary>
    private static IEnumerable<TargetedAbility> GetTargetedAbilities()
    {
        return GetAllAbilities().Where(x => x.Targeting != null);
    }

    /// <summary>
    /// Every ability, including those with no targeting metadata at all. The absence of metadata is
    /// itself the silent failure mode this fixture guards, so it must stay visible here.
    /// </summary>
    private static IEnumerable<TargetedAbility> GetAllAbilities()
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
                yield return new TargetedAbility(
                    definitionType.Name,
                    feat,
                    ability.Targeting,
                    ability.IsAreaAbility,
                    ability.IsHostileAbility,
                    ability.RequiresTarget,
                    ability.RequiresLocationTarget,
                    ability.HasExplicitMaxRange,
                    ability.SkillType,
                    ability.IsMimicryTechnique);
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

        // Mimicry techniques reach players through learning slots rather than GrantsFeat, so the
        // source scan alone silently skips every *Technique feat - the blind spot that let twenty
        // aimed techniques ship without a targeting cursor.
        foreach (var ability in GetAllAbilities().Where(x => x.IsMimicryTechnique))
        {
            feats.Add(ability.Feat);
        }

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

    private static Dictionary<string, string> ReadSpellRowLabels()
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, "SWLOR_Haks", "sw_2da", "spells.2da");
        var labels = new Dictionary<string, string>();
        foreach (var line in File.ReadAllLines(path).Skip(3))
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2 || tokens[1] == "****")
                continue;

            labels[tokens[0]] = tokens[1];
        }

        return labels;
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
