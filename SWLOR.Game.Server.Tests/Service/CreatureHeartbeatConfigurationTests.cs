using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Tests.Service;

public class CreatureHeartbeatConfigurationTests
{
    [Test]
    public void EngineeringGuildmasterPaletteUsesManagedHeartbeat()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, "Module", "utc", "eng_guildmaster.utc.json")));
        Heartbeat(document.RootElement).Should().Be(ScriptName.OnCreatureHeartbeatAfter);
    }

    [Test]
    public void EveryPlacedEngineeringGuildmasterUsesManagedHeartbeat()
    {
        var placements = 0;
        foreach (var path in Directory.EnumerateFiles(Path.Combine(Root, "Module", "git"), "*.git.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("Creature List", out var creatures))
                continue;
            foreach (var creature in creatures.GetProperty("value").EnumerateArray())
            {
                if (Value(creature, "Tag") != "eng_guildmaster" && Value(creature, "TemplateResRef") != "eng_guildmaster")
                    continue;
                placements++;
                Heartbeat(creature).Should().Be(ScriptName.OnCreatureHeartbeatAfter,
                    $"the placed Engineering Guild Master in {Path.GetFileName(path)} must avoid the legacy stock heartbeat");
            }
        }
        placements.Should().BeGreaterThan(0, "the regression must cover world placements as well as the palette blueprint");
    }

    private static string Heartbeat(JsonElement creature) => Value(creature, "ScriptHeartbeat");

    private static string Value(JsonElement element, string property) =>
        element.TryGetProperty(property, out var field) ? field.GetProperty("value").GetString() ?? "" : "";

    private static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Module")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new DirectoryNotFoundException("The module corpus could not be located.");
        }
    }
}
