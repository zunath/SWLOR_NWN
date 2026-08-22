using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class TeleportPlaceableConfigurationTests
{
    private const string PartyTeleportVariable = "TELEPORT_PARTY_MEMBERS";

    [Test]
    public void TeleportPlaceables_DefinePartyTeleportFlag()
    {
        var teleportObjects = LoadTeleportObjects();

        teleportObjects.Should().NotBeEmpty();
        teleportObjects.Should().OnlyContain(teleport => teleport.PartyTeleportFlag.HasValue);
    }

    [Test]
    public void CapstoneAccessGates_AreOnlyTeleportPlaceablesOptedIntoPartyTeleport()
    {
        var enabledTeleporters = LoadTeleportObjects()
            .Where(teleport => teleport.PartyTeleportFlag == 1)
            .Select(teleport => $"{NormalizePath(teleport.RelativePath)}|{teleport.Name}|{teleport.Destination}|{teleport.TemplateResRef}|{teleport.Tag}")
            .ToArray();

        enabledTeleporters.Should().BeEquivalentTo(new[]
        {
            "Module/git/veles_sewers.git.json|Enter Sewers Depths|VISC_SEWER_DEPTHS_INSIDE|tele_obj|tele_obj",
            "Module/git/dan_warehouse.git.json|Dantooine Medical Sublevel|to_medsublevel|tele_obj|tele_obj",
            "Module/git/dathgrottocavern.git.json|Enter the Grotto Apex Den|DATH_APEX_DEN_INSIDE|tele_obj|tele_obj",
            "Module/git/pw_ar_nsficlub.git.json|[Back Rooms]|SMUG_BACKROOMS|tele_obj|tele_obj",
            "Module/git/czs220_maintlvl.git.json|Enter the Breaker Bay|CZ220_MAINT_ENTRANCE_BREAKER|tele_obj|tele_obj",
            "Module/git/pw_ar_narscorpd.git.json|[Enter Czerka Weapons Testing Facility]|SMUG_CZERKA_WEAPONS_TEST_ENT|tele_obj|tele_obj",
        });

        LoadTeleportObjects()
            .Where(teleport => teleport.PartyTeleportFlag != 1)
            .Should()
            .OnlyContain(teleport => teleport.PartyTeleportFlag == 0);
    }

    // Teleporters whose destinations have never been built (missing districts, interiors, or an
    // inbound route that no longer exists). They fail with the in-game "inform an admin" message
    // today; resolving them needs a content decision (build the destination or remove the
    // placeable), not a retarget. Keyed by "relative path|LocName".
    private static readonly string[] KnownUnbuiltTeleporters =
    {
        "Module/git/druz_shalim.git.json|Invisible Object (Large)|Druzer_Starport_Inside",
        "Module/git/veles_sewers.git.json|Surface Access|SEWERS_VELES_INDUSTRIAL_INSIDE",
        "Module/git/veles_sewers.git.json|Surface Access|SEWERS_VELES_SLUMS",
        "Module/git/veles_sewers.git.json|Surface Access|SEWERS_VELES_SMUGGLER",
        "Module/git/zomb_abanstation.git.json|Take Shuttle Back to Viscara|",
    };

    [Test]
    public void TeleportPlaceables_DestinationsResolveToExistingTags()
    {
        var root = FindRepositoryRoot();
        var knownTags = Directory
            .EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.json", SearchOption.AllDirectories)
            .SelectMany(path =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                return EnumerateTags(document.RootElement).ToArray();
            })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var placedTeleports = LoadTeleportObjects()
            .Where(teleport => NormalizePath(teleport.RelativePath).StartsWith("Module/git/"))
            .ToArray();

        placedTeleports.Should().NotBeEmpty();
        var broken = placedTeleports
            .Where(teleport => !KnownUnbuiltTeleporters.Contains(
                $"{NormalizePath(teleport.RelativePath)}|{teleport.Name}|{teleport.Destination}",
                StringComparer.OrdinalIgnoreCase))
            .Where(teleport => string.IsNullOrWhiteSpace(teleport.Destination) || !knownTags.Contains(teleport.Destination))
            .Select(teleport => $"{NormalizePath(teleport.RelativePath)}|{teleport.Name}|DESTINATION='{teleport.Destination}'")
            .ToArray();

        broken.Should().BeEquivalentTo(
            Array.Empty<string>(),
            "every placed teleport's DESTINATION local string must match the tag of an object placed somewhere in Module/git");
    }

    private static IEnumerable<string> EnumerateTags(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var tag = GetString(element, "Tag");
            if (!string.IsNullOrEmpty(tag))
            {
                yield return tag;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateTags(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateTags(item))
                {
                    yield return nested;
                }
            }
        }
    }

    [Test]
    public void TeleportScript_UsesObjectRangeAndPlayerFacingNamesForPartyTeleport()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "PlaceableScripts.cs"));

        source.Should().Contain("private const float TeleportPartyMemberRange = 8.0f;");
        source.Should().Contain("GetLocalBool(device, \"TELEPORT_PARTY_MEMBERS\")");
        source.Should().Contain("GetDistanceBetween(partyMember, device) > TeleportPartyMemberRange");
        source.Should().Contain("IsInCombatOrHasEnmity(partyMember)");
        source.Should().Contain("PlayerName.GetDisplayName(partyMember, user)");
        source.Should().Contain("You ventured forth with {userName}.");
    }

    private static string NormalizePath(string path)
    {
        return path
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private static TeleportObject[] LoadTeleportObjects()
    {
        var root = FindRepositoryRoot();
        var searchRoots = new[]
        {
            Path.Combine(root.FullName, "Module", "git"),
            Path.Combine(root.FullName, "Module", "utp"),
        };

        return searchRoots
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.json", SearchOption.AllDirectories))
            .SelectMany(path => LoadTeleportObjects(root, path))
            .ToArray();
    }

    private static IEnumerable<TeleportObject> LoadTeleportObjects(DirectoryInfo root, string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var relativePath = Path.GetRelativePath(root.FullName, path);

        return EnumerateTeleportObjects(document.RootElement)
            .Select(json => new TeleportObject(
                relativePath,
                GetLocalizedString(json, "LocName"),
                GetString(json, "TemplateResRef"),
                GetString(json, "Tag"),
                GetLocalString(json, "DESTINATION"),
                GetLocalInt(json, PartyTeleportVariable)))
            .ToArray();
    }

    private static IEnumerable<JsonElement> EnumerateTeleportObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (GetString(element, "OnUsed") == "teleport")
            {
                yield return element;
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var teleportObject in EnumerateTeleportObjects(property.Value))
                {
                    yield return teleportObject;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var teleportObject in EnumerateTeleportObjects(item))
                {
                    yield return teleportObject;
                }
            }
        }
    }

    private static string GetString(JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) &&
               property.TryGetProperty("value", out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : string.Empty;
    }

    private static int? GetLocalInt(JsonElement json, string variableName)
    {
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() != variableName)
            {
                continue;
            }

            return variable.GetProperty("Value").GetProperty("value").GetInt32();
        }

        return null;
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() != variableName)
            {
                continue;
            }

            return variable.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string GetLocalizedString(JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) &&
               property.TryGetProperty("value", out var value) &&
               value.ValueKind == JsonValueKind.Object &&
               value.TryGetProperty("0", out var text) &&
               text.ValueKind == JsonValueKind.String
            ? text.GetString()!
            : string.Empty;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
        {
            directory = directory.Parent;
        }

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed record TeleportObject(
        string RelativePath,
        string Name,
        string TemplateResRef,
        string Tag,
        string Destination,
        int? PartyTeleportFlag);
}
