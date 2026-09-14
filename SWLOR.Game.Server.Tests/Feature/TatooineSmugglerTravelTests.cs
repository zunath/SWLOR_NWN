using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Tests.Feature;

public class TatooineSmugglerTravelTests
{
    [Test]
    public void SmugglerShipTeleport_RequiresTheQuestRewardedSmugglerPass()
    {
        using var area = LoadModuleJson("git", "tat_anc_astropor.git.json");

        var teleporter = EnumerateObjects(area.RootElement)
            .Single(element =>
                GetString(element, "OnUsed") == "teleport" &&
                GetOptionalLocalString(element, "DESTINATION") == "smuggler_moon");

        GetLocalInt(teleporter, "KEY_ITEM_ID").Should().Be((int)KeyItemType.SmugglerPass);
        GetLocalString(teleporter, "MISSING_KEY_ITEM_MESSAGE").Should().Contain("key pass");
    }

    private static JsonDocument LoadModuleJson(string folder, string fileName)
    {
        var root = FindRepositoryRoot();
        return JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root.FullName,
            "Module",
            folder,
            fileName)));
    }

    private static int GetLocalInt(JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetInt32();
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        return GetLocal(json, variableName).GetProperty("Value").GetProperty("value").GetString()!;
    }

    private static string GetOptionalLocalString(JsonElement json, string variableName)
    {
        return TryGetLocal(json, variableName, out var local)
            ? local.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty
            : string.Empty;
    }

    private static JsonElement GetLocal(JsonElement json, string variableName)
    {
        if (!TryGetLocal(json, variableName, out var local))
            throw new InvalidOperationException($"Missing local variable '{variableName}'.");

        return local;
    }

    private static bool TryGetLocal(JsonElement json, string variableName, out JsonElement local)
    {
        local = default;
        if (!json.TryGetProperty("VarTable", out var varTable) ||
            !varTable.TryGetProperty("value", out var variables) ||
            variables.ValueKind != JsonValueKind.Array)
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

    private static string GetString(JsonElement json, string propertyName)
    {
        return json.TryGetProperty(propertyName, out var property) &&
               property.TryGetProperty("value", out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : string.Empty;
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            {
                foreach (var nested in EnumerateObjects(property.Value))
                    yield return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nested in EnumerateObjects(item))
                    yield return nested;
            }
        }
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
