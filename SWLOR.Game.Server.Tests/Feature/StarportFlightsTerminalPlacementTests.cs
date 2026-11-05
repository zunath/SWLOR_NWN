using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Tests.Feature;

public class StarportFlightsTerminalPlacementTests
{
    private const string TerminalTag = "flights_terminal";
    private const string PlanetVariable = "CURRENT_LOCATION";
    private const string GenericTerminalName = "Starport Flights Terminal";

    [OneTimeSetUp]
    public void CachePlanets()
    {
        typeof(Planet).GetMethod("CachePlanets", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, null);
    }

    [Test]
    public void SmugglersMoonLandingPadTerminals_AreNamedForSmugglersMoon()
    {
        var terminals = LoadTerminals("pw_ar_nsshipyard");

        terminals.Should().HaveCount(6);
        terminals.Should().OnlyContain(t =>
            t.Name == "Smuggler's Moon Starport Flights Terminal" &&
            t.Planet == PlanetType.SmugglersMoon &&
            t.Conversation == "StarportFlightsDialog");
    }

    [Test]
    public void PlacedTerminals_AreNamedForThePlanetTheySellFlightsFrom()
    {
        var root = FindRepositoryRoot();
        var failures = new List<string>();

        foreach (var gitPath in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "git"), "*.git.json"))
        {
            var text = File.ReadAllText(gitPath);
            if (!text.Contains(TerminalTag, StringComparison.Ordinal))
                continue;

            var resref = Path.GetFileName(gitPath)[..^".git.json".Length];
            using var git = JsonDocument.Parse(text);
            foreach (var terminal in ReadTerminals(git.RootElement))
            {
                // A generic name is vague but not wrong; a planet name must be the terminal's own origin.
                // The station's lone terminal is named after the station itself.
                var planetName = Planet.GetPlanetByType(terminal.Planet).Name;
                if (terminal.Name != GenericTerminalName &&
                    terminal.Name != planetName &&
                    terminal.Name != $"{planetName} {GenericTerminalName}")
                    failures.Add($"{resref}: '{terminal.Name}' sells flights from {planetName}");
            }
        }

        failures.Should().BeEmpty();
    }

    private sealed record Terminal(string Name, PlanetType Planet, string Conversation);

    private static List<Terminal> LoadTerminals(string areaResref)
    {
        var path = Path.Combine(FindRepositoryRoot().FullName, "Module", "git", $"{areaResref}.git.json");
        using var git = JsonDocument.Parse(File.ReadAllText(path));
        return ReadTerminals(git.RootElement);
    }

    private static List<Terminal> ReadTerminals(JsonElement git)
    {
        if (!git.TryGetProperty("Placeable List", out var placeables))
            return new List<Terminal>();

        return placeables.GetProperty("value").EnumerateArray()
            .Where(p => GetString(p, "Tag") == TerminalTag)
            .Select(p => new Terminal(
                GetString(p, "LocName"),
                (PlanetType)GetLocalInt(p, PlanetVariable),
                GetLocalString(p, "CONVERSATION")))
            .ToList();
    }

    private static int GetLocalInt(JsonElement placeable, string name)
    {
        var value = FindLocal(placeable, name);
        return value?.GetInt32() ?? 0;
    }

    private static string GetLocalString(JsonElement placeable, string name)
    {
        var value = FindLocal(placeable, name);
        return value?.GetString() ?? string.Empty;
    }

    private static JsonElement? FindLocal(JsonElement placeable, string name)
    {
        if (!placeable.TryGetProperty("VarTable", out var table))
            return null;

        foreach (var variable in table.GetProperty("value").EnumerateArray())
        {
            if (GetString(variable, "Name") == name)
                return variable.GetProperty("Value").GetProperty("value");
        }

        return null;
    }

    private static string GetString(JsonElement json, string propertyName)
    {
        if (!json.TryGetProperty(propertyName, out var property) ||
            !property.TryGetProperty("value", out var value))
        {
            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
            return value.GetString()!;

        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("0", out var localized))
            return localized.GetString()!;

        return string.Empty;
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
