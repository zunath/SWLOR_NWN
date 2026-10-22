using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;

namespace SWLOR.Game.Server.Tests.Feature;

/// <summary>
/// Searched scavenge points with leftover items despawn from the close handler, and emptied ones
/// queue their respawn from the death handler, so every scavenge blueprint must run the shared
/// open, close, disturb, and death scripts.
/// </summary>
public class ScavengePointBlueprintTests
{
    [Test]
    public void EveryScavengePointUsesTheSharedScavengeScripts()
    {
        var root = FindRepositoryRoot();
        var scavengePoints = new List<string>();
        var problems = new List<string>();

        foreach (var file in Directory.EnumerateFiles(Path.Combine(root.FullName, "Module", "utp"), "*.utp.json"))
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("SCAVENGE_POINT_LEVEL") && !text.Contains("\"scav_opened\""))
                continue;

            using var blueprint = JsonDocument.Parse(text);
            var name = Path.GetFileName(file);
            scavengePoints.Add(name);

            foreach (var (field, script) in new[]
                     {
                         ("OnOpen", "scav_opened"),
                         ("OnClosed", "scav_closed"),
                         ("OnInvDisturbed", "scav_disturbed"),
                         ("OnDeath", "plc_death")
                     })
            {
                var actual = blueprint.RootElement.GetProperty(field).GetProperty("value").GetString();
                if (actual != script)
                    problems.Add($"{name} {field} is '{actual}', expected '{script}'.");
            }
        }

        scavengePoints.Should().NotBeEmpty();
        problems.Should().BeEmpty();
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
