using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Feature;

public class FightClubBackroomsPlacementTests
{
    private const string FightClubAreaFile = "pw_ar_nsficlub.git.json";
    private const string BackroomsAreaFile = "pw_sc_emfbackr.git.json";

    [Test]
    public void BackroomsAccessDoor_IsGatedByTheCapstoneKeyItem()
    {
        ((int)KeyItemType.CapstoneSmugglersMoonFightClubBackroomsKey).Should().Be(110);

        using var club = LoadModuleJson("git", FightClubAreaFile);

        var gate = EnumerateObjects(club.RootElement)
            .Single(element =>
                GetString(element, "OnUsed") == "teleport" &&
                GetOptionalLocalString(element, "DESTINATION") == "SMUG_BACKROOMS");

        GetLocalInt(gate, "KEY_ITEM_ID").Should().Be((int)KeyItemType.CapstoneSmugglersMoonFightClubBackroomsKey);
        GetLocalInt(gate, "TELEPORT_PARTY_MEMBERS").Should().Be(1);
        GetLocalString(gate, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("Fight Club Backrooms");
        GetLocalString(gate, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("key");
    }

    [Test]
    public void BackroomsDungeon_UsesGeneratedCapstoneSpawnTable()
    {
        using var backrooms = LoadModuleJson("git", BackroomsAreaFile);

        GetLocalString(backrooms.RootElement, "CREATURE_SPAWN_TABLE_ID")
            .Should().Be("CAPSTONE_SMUGGLERS_MOON_FIGHT_CLUB_BACKROOMS");
        GetLocalInt(backrooms.RootElement, "IS_DUNGEON").Should().Be(1);

        backrooms.RootElement
            .GetProperty("WaypointList")
            .GetProperty("value")
            .EnumerateArray()
            .Select(waypoint => GetString(waypoint, "Tag"))
            .Should().Contain("STUCK_WAYPOINT");
    }

    [Test]
    public void FightClubFloor_HoldsNoQuestGivers()
    {
        // Quest giver placement is owned by CapstoneQuestGiverPlacementTests; the Fight Club floor
        // has arena-fighter spawn waypoints, so no trainer may stand on it.
        using var club = LoadModuleJson("git", FightClubAreaFile);
        club.RootElement
            .GetProperty("Creature List")
            .GetProperty("value")
            .EnumerateArray()
            .Select(creature => GetString(creature, "TemplateResRef"))
            .Should()
            .NotContain(new[] { "cq_cripdef", "cq_tempbloom", "cq_redbloom" });
    }

    private static System.Text.Json.JsonDocument LoadModuleJson(string folder, string fileName)
    {
        var root = FindRepositoryRoot();
        return System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            folder,
            fileName)));
    }

    private static string GetLocalString(System.Text.Json.JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetString()!;
    }

    private static string GetOptionalLocalString(System.Text.Json.JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            return string.Empty;

        return local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
    }

    private static int GetLocalInt(System.Text.Json.JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static System.Text.Json.JsonElement GetLocal(System.Text.Json.JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            throw new InvalidOperationException($"Missing local variable '{variableName}'.");

        return local;
    }

    private static bool TryGetLocal(
        System.Text.Json.JsonElement json,
        string variableName,
        out System.Text.Json.JsonElement local)
    {
        local = default;

        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != System.Text.Json.JsonValueKind.Array)
        {
            return false;
        }

        foreach (var variable in variables.EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                local = variable;
                return true;
            }
        }

        return false;
    }

    private static string GetString(System.Text.Json.JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) &&
               property.TryGetProperty("value", out var value) &&
               value.ValueKind == System.Text.Json.JsonValueKind.String
            ? value.GetString()!
            : string.Empty;
    }

    private static IEnumerable<System.Text.Json.JsonElement> EnumerateObjects(System.Text.Json.JsonElement element)
    {
        if (element.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            yield return element;

            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateObjects(property.Value))
                {
                    yield return nested;
                }
            }
        }
        else if (element.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateObjects(item))
                {
                    yield return nested;
                }
            }
        }
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
}
