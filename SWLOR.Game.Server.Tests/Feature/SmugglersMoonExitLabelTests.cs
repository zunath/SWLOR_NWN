using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

public class SmugglersMoonExitLabelTests
{
    private static readonly string[] SourceAreas =
    {
        "narcatwalk", "nardocks", "narpromena", "nars_canhd", "narscorpd", "narshahub", "narslum",
        "nscasino", "nsczgnstr", "nsficlub", "nsgsidunge", "nsshipyard"
    };

    private static readonly Dictionary<string, string[]> LabelsByDestinationArea = new()
    {
        ["bhbar"] = new[] { "[To Nomads]" },
        ["czoffice"] = new[] { "[To Czerka Office]" },
        ["narcatwalk"] = new[] { "[To Catwalks]" },
        ["nardocks"] = new[] { "[To Shipping District]" },
        ["narpromena"] = new[] { "[To Promenade]" },
        ["narscorpd"] = new[] { "[To Corporate District]" },
        ["narshahub"] = new[] { "[To Hub]" },
        ["narslum"] = new[] { "[To Slums]" },
        ["ns_doffice"] = new[] { "[To Foreman Shack]" },
        ["nscasino"] = new[] { "[To Casino]", "[To Casino Floor]", "[To Backroom]" },
        ["nsczgnstr"] = new[] { "[To Czerka Arms Store]" },
        ["nsgsidunge"] = new[] { "[To GSI Tower]" },
        ["nsshipyard"] = new[] { "[To Shipyard]", "[To Landing]" }
    };

    [Test]
    public void SmugglersMoonExitLabels_NameTheAreaTheExitLeadsTo()
    {
        var gitDirectory = Path.Combine(FindRepositoryRoot().FullName, "Module", "git");
        var waypointAreas = new Dictionary<string, string>();
        var labels = new List<(string Area, string Label, string Destination)>();

        foreach (var area in SourceAreas.Concat(LabelsByDestinationArea.Keys).Distinct())
        {
            var path = Path.Combine(gitDirectory, $"pw_ar_{area}.git.json");
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;

            foreach (var waypoint in GetList(root, "WaypointList"))
            {
                waypointAreas[GetString(waypoint, "Tag")] = area;
            }

            if (!SourceAreas.Contains(area))
            {
                continue;
            }

            foreach (var placeable in GetList(root, "Placeable List"))
            {
                var label = GetLabel(placeable);
                if (!label.StartsWith("[To"))
                {
                    continue;
                }

                labels.Add((area, label, GetLocalString(placeable, "DESTINATION")));
            }
        }

        labels.Should().NotBeEmpty();
        foreach (var (area, label, destination) in labels)
        {
            waypointAreas.Should().ContainKey(destination, $"'{label}' in {area} must lead to an existing waypoint");
            var destinationArea = waypointAreas[destination];
            LabelsByDestinationArea.Should().ContainKey(destinationArea);
            LabelsByDestinationArea[destinationArea].Should().Contain(label,
                $"the exit in {area} to {destination} leads to {destinationArea}");
        }
    }

    private static IEnumerable<JsonElement> GetList(JsonElement area, string name)
    {
        return area.TryGetProperty(name, out var list) ? list.GetProperty("value").EnumerateArray() : Enumerable.Empty<JsonElement>();
    }

    private static string GetLabel(JsonElement placeable)
    {
        if (placeable.TryGetProperty("LocName", out var name) &&
            name.TryGetProperty("value", out var value) &&
            value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty("0", out var text))
        {
            return text.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static string GetString(JsonElement json, string name)
    {
        return json.GetProperty(name).GetProperty("value").GetString() ?? string.Empty;
    }

    private static string GetLocalString(JsonElement json, string variableName)
    {
        foreach (var variable in json.GetProperty("VarTable").GetProperty("value").EnumerateArray())
        {
            if (variable.GetProperty("Name").GetProperty("value").GetString() == variableName)
            {
                return variable.GetProperty("Value").GetProperty("value").GetString() ?? string.Empty;
            }
        }

        return string.Empty;
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
